using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using TypingCore.Abstractions;
using TypingCore.Models;

namespace TypingCore.Persistence;

/// <summary>
/// Stores article records in a SQLite database.
/// </summary>
/// <remarks>
/// Instances are safe for concurrent use when callers provide an externally synchronized connection string target.
/// </remarks>
public sealed class SqliteArticleRepository : IArticleRepository
{
    private readonly SqliteDatabase database;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteArticleRepository"/> class.
    /// </summary>
    /// <param name="connectionString">The SQLite connection string.</param>
    public SqliteArticleRepository(string connectionString)
    {
        database = new SqliteDatabase(connectionString);
    }

    /// <inheritdoc />
    public async Task SaveAsync(IArticleRecord articleRecord, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(articleRecord);

        await using SqliteConnection connection =
            await database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO articles(article_id, title, raw_text, created_at, tags_json)
            VALUES(@articleId, @title, @rawText, @createdAt, @tagsJson)
            ON CONFLICT(article_id) DO UPDATE SET
                title = excluded.title,
                raw_text = excluded.raw_text,
                created_at = excluded.created_at,
                tags_json = excluded.tags_json;
            """;
        command.Parameters.AddWithValue("@articleId", articleRecord.ArticleId);
        command.Parameters.AddWithValue("@title", articleRecord.Title);
        command.Parameters.AddWithValue("@rawText", articleRecord.RawText);
        command.Parameters.AddWithValue("@createdAt", SqliteDatabase.FormatTimestamp(articleRecord.CreatedAt));
        command.Parameters.AddWithValue("@tagsJson", SerializeTags(articleRecord.Tags));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string articleId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(articleId);

        await using SqliteConnection connection =
            await database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "DELETE FROM articles WHERE article_id = @articleId;";
        command.Parameters.AddWithValue("@articleId", articleId);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IArticleRecord?> GetByIdAsync(
        string articleId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(articleId);

        await using SqliteConnection connection =
            await database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT article_id, title, raw_text, created_at, tags_json
            FROM articles
            WHERE article_id = @articleId;
            """;
        command.Parameters.AddWithValue("@articleId", articleId);

        await using SqliteDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return ReadArticle(reader);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IArticleRecord>> SearchAsync(
        string? query = null,
        string? tag = null,
        CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection =
            await database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using SqliteCommand command = connection.CreateCommand();

        StringBuilder sql = new(
            """
            SELECT article_id, title, raw_text, created_at, tags_json
            FROM articles
            """);

        List<string> filters = new();

        if (!string.IsNullOrWhiteSpace(query))
        {
            filters.Add("(title LIKE @pattern ESCAPE '\\' OR raw_text LIKE @pattern ESCAPE '\\')");
            command.Parameters.AddWithValue("@pattern", $"%{EscapeLikePattern(query)}%");
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            filters.Add("tags_json LIKE @tagPattern ESCAPE '\\'");
            command.Parameters.AddWithValue(
                "@tagPattern",
                $"%{EscapeLikePattern(JsonSerializer.Serialize(tag))}%");
        }

        if (filters.Count > 0)
        {
            sql.AppendLine();
            sql.Append("WHERE ");
            sql.Append(string.Join(" AND ", filters));
        }

        sql.AppendLine();
        sql.Append("ORDER BY created_at DESC, article_id ASC;");
        command.CommandText = sql.ToString();

        List<IArticleRecord> results = new();
        await using SqliteDataReader reader =
            await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            results.Add(ReadArticle(reader));
        }

        return results;
    }

    private static Article ReadArticle(SqliteDataReader reader)
    {
        int articleIdOrdinal = reader.GetOrdinal("article_id");
        int titleOrdinal = reader.GetOrdinal("title");
        int rawTextOrdinal = reader.GetOrdinal("raw_text");
        int createdAtOrdinal = reader.GetOrdinal("created_at");
        int tagsOrdinal = reader.GetOrdinal("tags_json");

        return new Article(
            reader.GetString(articleIdOrdinal),
            reader.GetString(titleOrdinal),
            reader.GetString(rawTextOrdinal),
            SqliteDatabase.ParseTimestamp(reader.GetString(createdAtOrdinal)),
            DeserializeTags(reader.GetString(tagsOrdinal)));
    }

    private static string SerializeTags(IReadOnlyCollection<string> tags)
        => JsonSerializer.Serialize(tags);

    private static IReadOnlyCollection<string> DeserializeTags(string json)
        => JsonSerializer.Deserialize<string[]>(json) ?? Array.Empty<string>();

    private static string EscapeLikePattern(string value)
        => value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}