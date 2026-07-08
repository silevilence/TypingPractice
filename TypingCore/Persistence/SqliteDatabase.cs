using System.Globalization;
using Microsoft.Data.Sqlite;

namespace TypingCore.Persistence;

internal sealed class SqliteDatabase
{
    private const int CurrentSchemaVersion = 3;

    private readonly string connectionString;

    public SqliteDatabase(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        this.connectionString = connectionString;
    }

    public async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        SqliteConnection connection = new(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using SqliteCommand foreignKeysCommand = connection.CreateCommand();
        foreignKeysCommand.CommandText = "PRAGMA foreign_keys = ON;";
        await foreignKeysCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        await EnsureInitializedAsync(connection, cancellationToken).ConfigureAwait(false);

        return connection;
    }

    public static string FormatTimestamp(DateTimeOffset value)
        => value.ToString("O", CultureInfo.InvariantCulture);

    public static DateTimeOffset ParseTimestamp(string value)
        => DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    private static async Task EnsureInitializedAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        int version = await ReadUserVersionAsync(connection, cancellationToken).ConfigureAwait(false);

        if (version > CurrentSchemaVersion)
        {
            throw new InvalidOperationException(
                $"Unsupported SQLite schema version {version}. Expected <= {CurrentSchemaVersion}.");
        }

        if (version == CurrentSchemaVersion)
        {
            return;
        }

        await using SqliteTransaction transaction =
            (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (version < 1)
            {
                await ApplyVersion1Async(connection, transaction, cancellationToken).ConfigureAwait(false);
            }

            if (version < 2)
            {
                await ApplyVersion2Async(connection, transaction, cancellationToken).ConfigureAwait(false);
            }

            if (version < 3)
            {
                await ApplyVersion3Async(connection, transaction, cancellationToken).ConfigureAwait(false);
            }

            await SetUserVersionAsync(connection, transaction, CurrentSchemaVersion, cancellationToken)
                .ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    private static async Task<int> ReadUserVersionAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";

        object? value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        return Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    private static async Task SetUserVersionAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        int version,
        CancellationToken cancellationToken)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"PRAGMA user_version = {version};";

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task ApplyVersion1Async(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        // Phase 3 keeps cross-table cleanup rules at the application layer so
        // article deletion and future keystroke retention policies can evolve
        // without forcing a schema rewrite this early.
        await using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS articles (
                article_id TEXT PRIMARY KEY,
                title TEXT NOT NULL,
                raw_text TEXT NOT NULL,
                created_at TEXT NOT NULL,
                tags_json TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS sessions (
                session_id TEXT PRIMARY KEY,
                article_id TEXT NOT NULL,
                started_at TEXT NOT NULL,
                ended_at TEXT NULL,
                keystrokes_per_minute REAL NULL,
                characters_per_minute REAL NULL,
                words_per_minute REAL NULL,
                average_code_length REAL NULL,
                backspace_count INTEGER NULL,
                error_rate REAL NULL,
                elapsed_ticks INTEGER NULL
            );

            CREATE INDEX IF NOT EXISTS idx_sessions_article_id_started_at
                ON sessions(article_id, started_at DESC);

            CREATE INDEX IF NOT EXISTS idx_sessions_started_at
                ON sessions(started_at);

            CREATE TABLE IF NOT EXISTS keystrokes (
                session_id TEXT NOT NULL,
                sequence INTEGER NOT NULL,
                observed_at TEXT NOT NULL,
                key INTEGER NOT NULL,
                is_from_ime INTEGER NOT NULL,
                ime_commit_text TEXT NULL,
                is_backspace INTEGER NOT NULL,
                PRIMARY KEY (session_id, sequence)
            );

            CREATE TABLE IF NOT EXISTS codetables (
                name TEXT PRIMARY KEY,
                source TEXT NOT NULL,
                loaded_at TEXT NOT NULL,
                entries_json TEXT NOT NULL
            );
            """;

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task ApplyVersion2Async(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "ALTER TABLE sessions ADD COLUMN backspace_rate REAL NULL;";

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task ApplyVersion3Async(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "ALTER TABLE articles ADD COLUMN is_deleted INTEGER NOT NULL DEFAULT 0;";

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
