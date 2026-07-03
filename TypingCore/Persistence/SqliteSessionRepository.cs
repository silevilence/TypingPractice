using Microsoft.Data.Sqlite;
using TypingCore.Abstractions;
using TypingCore.Models;

namespace TypingCore.Persistence;

/// <summary>
/// Stores typing session records in a SQLite database.
/// </summary>
/// <remarks>
/// Instances are safe for concurrent use when callers provide an externally synchronized connection string target.
/// </remarks>
public sealed class SqliteSessionRepository : ISessionRepository
{
    private readonly SqliteDatabase database;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteSessionRepository"/> class.
    /// </summary>
    /// <param name="connectionString">The SQLite connection string.</param>
    public SqliteSessionRepository(string connectionString)
    {
        database = new SqliteDatabase(connectionString);
    }

    /// <inheritdoc />
    public async Task SaveAsync(ISessionRecord sessionRecord, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sessionRecord);

        await using SqliteConnection connection =
            await database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO sessions(
                session_id,
                article_id,
                started_at,
                ended_at,
                keystrokes_per_minute,
                characters_per_minute,
                words_per_minute,
                average_code_length,
                backspace_count,
                error_rate,
                elapsed_ticks)
            VALUES(
                @sessionId,
                @articleId,
                @startedAt,
                @endedAt,
                @keystrokesPerMinute,
                @charactersPerMinute,
                @wordsPerMinute,
                @averageCodeLength,
                @backspaceCount,
                @errorRate,
                @elapsedTicks)
            ON CONFLICT(session_id) DO UPDATE SET
                article_id = excluded.article_id,
                started_at = excluded.started_at,
                ended_at = excluded.ended_at,
                keystrokes_per_minute = excluded.keystrokes_per_minute,
                characters_per_minute = excluded.characters_per_minute,
                words_per_minute = excluded.words_per_minute,
                average_code_length = excluded.average_code_length,
                backspace_count = excluded.backspace_count,
                error_rate = excluded.error_rate,
                elapsed_ticks = excluded.elapsed_ticks;
            """;
        command.Parameters.AddWithValue("@sessionId", sessionRecord.SessionId);
        command.Parameters.AddWithValue("@articleId", sessionRecord.ArticleId);
        command.Parameters.AddWithValue("@startedAt", SqliteDatabase.FormatTimestamp(sessionRecord.StartedAt));
        command.Parameters.AddWithValue(
            "@endedAt",
            sessionRecord.EndedAt is null
                ? DBNull.Value
                : SqliteDatabase.FormatTimestamp(sessionRecord.EndedAt.Value));
        command.Parameters.AddWithValue(
            "@keystrokesPerMinute",
            sessionRecord.Statistics is null ? DBNull.Value : sessionRecord.Statistics.KeystrokesPerMinute);
        command.Parameters.AddWithValue(
            "@charactersPerMinute",
            sessionRecord.Statistics is null ? DBNull.Value : sessionRecord.Statistics.CharactersPerMinute);
        command.Parameters.AddWithValue(
            "@wordsPerMinute",
            sessionRecord.Statistics is null ? DBNull.Value : sessionRecord.Statistics.WordsPerMinute);
        command.Parameters.AddWithValue(
            "@averageCodeLength",
            sessionRecord.Statistics is null ? DBNull.Value : sessionRecord.Statistics.AverageCodeLength);
        command.Parameters.AddWithValue(
            "@backspaceCount",
            sessionRecord.Statistics is null ? DBNull.Value : sessionRecord.Statistics.BackspaceCount);
        command.Parameters.AddWithValue(
            "@errorRate",
            sessionRecord.Statistics is null ? DBNull.Value : sessionRecord.Statistics.ErrorRate);
        command.Parameters.AddWithValue(
            "@elapsedTicks",
            sessionRecord.Statistics is null ? DBNull.Value : sessionRecord.Statistics.Elapsed.Ticks);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ISessionRecord>> GetByArticleIdAsync(
        string articleId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(articleId);

        await using SqliteConnection connection =
            await database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                session_id,
                article_id,
                started_at,
                ended_at,
                keystrokes_per_minute,
                characters_per_minute,
                words_per_minute,
                average_code_length,
                backspace_count,
                error_rate,
                elapsed_ticks
            FROM sessions
            WHERE article_id = @articleId
            ORDER BY started_at DESC, session_id ASC;
            """;
        command.Parameters.AddWithValue("@articleId", articleId);

        return await ReadSessionsAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ISessionRecord>> GetByTimeRangeAsync(
        DateTimeOffset startInclusive,
        DateTimeOffset endExclusive,
        CancellationToken cancellationToken = default)
    {
        if (endExclusive < startInclusive)
        {
            throw new ArgumentOutOfRangeException(
                nameof(endExclusive),
                "The end of the range must be greater than or equal to the start.");
        }

        await using SqliteConnection connection =
            await database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                session_id,
                article_id,
                started_at,
                ended_at,
                keystrokes_per_minute,
                characters_per_minute,
                words_per_minute,
                average_code_length,
                backspace_count,
                error_rate,
                elapsed_ticks
            FROM sessions
            WHERE started_at >= @startInclusive
              AND started_at < @endExclusive
            ORDER BY started_at ASC, session_id ASC;
            """;
        command.Parameters.AddWithValue("@startInclusive", SqliteDatabase.FormatTimestamp(startInclusive));
        command.Parameters.AddWithValue("@endExclusive", SqliteDatabase.FormatTimestamp(endExclusive));

        return await ReadSessionsAsync(command, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyList<ISessionRecord>> ReadSessionsAsync(
        SqliteCommand command,
        CancellationToken cancellationToken)
    {
        List<ISessionRecord> sessions = new();
        await using SqliteDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            sessions.Add(ReadSession(reader));
        }

        return sessions;
    }

    private static TypingSessionRecord ReadSession(SqliteDataReader reader)
    {
        int sessionIdOrdinal = reader.GetOrdinal("session_id");
        int articleIdOrdinal = reader.GetOrdinal("article_id");
        int startedAtOrdinal = reader.GetOrdinal("started_at");
        int endedAtOrdinal = reader.GetOrdinal("ended_at");
        int keystrokesPerMinuteOrdinal = reader.GetOrdinal("keystrokes_per_minute");
        int charactersPerMinuteOrdinal = reader.GetOrdinal("characters_per_minute");
        int wordsPerMinuteOrdinal = reader.GetOrdinal("words_per_minute");
        int averageCodeLengthOrdinal = reader.GetOrdinal("average_code_length");
        int backspaceCountOrdinal = reader.GetOrdinal("backspace_count");
        int errorRateOrdinal = reader.GetOrdinal("error_rate");
        int elapsedTicksOrdinal = reader.GetOrdinal("elapsed_ticks");

        SessionStatistics? statistics = null;

        if (!reader.IsDBNull(keystrokesPerMinuteOrdinal))
        {
            statistics = new SessionStatistics(
                reader.GetDouble(keystrokesPerMinuteOrdinal),
                reader.GetDouble(charactersPerMinuteOrdinal),
                reader.GetDouble(wordsPerMinuteOrdinal),
                reader.GetDouble(averageCodeLengthOrdinal),
                reader.GetInt32(backspaceCountOrdinal),
                reader.GetDouble(errorRateOrdinal),
                TimeSpan.FromTicks(reader.GetInt64(elapsedTicksOrdinal)));
        }

        return new TypingSessionRecord(
            reader.GetString(sessionIdOrdinal),
            reader.GetString(articleIdOrdinal),
            SqliteDatabase.ParseTimestamp(reader.GetString(startedAtOrdinal)),
            reader.IsDBNull(endedAtOrdinal)
                ? null
                : SqliteDatabase.ParseTimestamp(reader.GetString(endedAtOrdinal)),
            statistics);
    }
}