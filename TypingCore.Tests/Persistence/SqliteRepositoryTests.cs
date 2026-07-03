using Microsoft.Data.Sqlite;
using TypingCore.Abstractions;
using TypingCore.Models;
using TypingCore.Persistence;

namespace TypingCore.Tests.Persistence;

public sealed class SqliteRepositoryTests : IDisposable
{
    private readonly string databasePath = Path.Combine(
        Path.GetTempPath(),
        $"typing-practice-tests-{Guid.NewGuid():N}.db");

    private string ConnectionString => new SqliteConnectionStringBuilder
    {
        DataSource = databasePath,
        Pooling = false,
    }.ToString();

    [Fact]
    public async Task Article_repository_supports_save_search_update_and_delete()
    {
        IArticleRepository repository = new SqliteArticleRepository(ConnectionString);
        DateTimeOffset createdAt = new(2026, 7, 3, 9, 0, 0, TimeSpan.Zero);

        Article initial = new(
            "article-1",
            "五笔入门",
            "春眠不觉晓",
            createdAt,
            new[] { "古诗", "五笔" });
        Article another = new(
            "article-2",
            "五笔练习二",
            "夜来风雨声",
            createdAt.AddMinutes(1),
            new[] { "散文" });

        await repository.SaveAsync(initial);
        await repository.SaveAsync(another);

        IArticleRecord? saved = await repository.GetByIdAsync("article-1");

        Assert.NotNull(saved);
        Assert.Equal("五笔入门", saved!.Title);
        Assert.Equal(new[] { "古诗", "五笔" }, saved.Tags);
        Assert.Equal(2, (await repository.SearchAsync(query: "五笔")).Count);
        Assert.Single(await repository.SearchAsync(tag: "古诗"));
        Assert.Single(await repository.SearchAsync(query: "五笔", tag: "古诗"));

        Article updated = new(
            "article-1",
            "双拼进阶",
            "处处闻啼鸟",
            createdAt.AddMinutes(5),
            new[] { "双拼" });

        await repository.SaveAsync(updated);

        saved = await repository.GetByIdAsync("article-1");

        Assert.NotNull(saved);
        Assert.Equal("双拼进阶", saved!.Title);
        Assert.Equal("处处闻啼鸟", saved.RawText);
        Assert.Equal(updated.CreatedAt, saved.CreatedAt);
        Assert.Equal(new[] { "双拼" }, saved.Tags);
        Assert.Empty(await repository.SearchAsync(tag: "古诗"));
        Assert.Single(await repository.SearchAsync(tag: "双拼"));
        Assert.Empty(await repository.SearchAsync(query: "五笔", tag: "古诗"));
        Assert.Single(await repository.SearchAsync(query: "五笔", tag: "散文"));

        await repository.DeleteAsync("article-1");

        Assert.Null(await repository.GetByIdAsync("article-1"));
    }

    [Fact]
    public async Task Session_repository_saves_statistics_and_queries_by_article_and_time_range()
    {
        ISessionRepository repository = new SqliteSessionRepository(ConnectionString);
        DateTimeOffset startedAt = new(2026, 7, 3, 10, 0, 0, TimeSpan.Zero);
        SessionStatistics statistics = new(
            318,
            264,
            53,
            4.3,
            2,
            0.03,
            TimeSpan.FromMinutes(2));

        await repository.SaveAsync(new TypingSessionRecord(
            "session-1",
            "article-1",
            startedAt,
            startedAt.AddMinutes(2),
            statistics));

        await repository.SaveAsync(new TypingSessionRecord(
            "session-2",
            "article-2",
            startedAt.AddHours(1),
            startedAt.AddHours(1).AddMinutes(2),
            null));

        ISessionRecord byArticle = Assert.Single(await repository.GetByArticleIdAsync("article-1"));
        ISessionRecord withoutStatistics = Assert.Single(await repository.GetByArticleIdAsync("article-2"));

        Assert.Equal("session-1", byArticle.SessionId);
        Assert.NotNull(byArticle.Statistics);
        Assert.Equal(318, byArticle.Statistics!.KeystrokesPerMinute);
        Assert.Equal(TimeSpan.FromMinutes(2), byArticle.Statistics.Elapsed);
        Assert.Null(withoutStatistics.Statistics);

        IReadOnlyList<ISessionRecord> inRange = await repository.GetByTimeRangeAsync(
            startedAt.AddMinutes(-1),
            startedAt.AddMinutes(10));

        ISessionRecord ranged = Assert.Single(inRange);

        Assert.Equal("session-1", ranged.SessionId);
        Assert.Empty(await repository.GetByTimeRangeAsync(startedAt, startedAt));
    }

    [Fact]
    public async Task Session_repository_rejects_inverted_time_range()
    {
        ISessionRepository repository = new SqliteSessionRepository(ConnectionString);
        DateTimeOffset start = new(2026, 7, 3, 12, 0, 0, TimeSpan.Zero);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            repository.GetByTimeRangeAsync(start, start.AddMinutes(-1)));
    }

    [Fact]
    public async Task Repository_initialization_creates_expected_schema_and_version()
    {
        IArticleRepository repository = new SqliteArticleRepository(ConnectionString);

        await repository.SearchAsync();

        HashSet<string> tableNames = await ReadUserTableNamesAsync();

        Assert.Contains("articles", tableNames);
        Assert.Contains("sessions", tableNames);
        Assert.Contains("keystrokes", tableNames);
        Assert.Contains("codetables", tableNames);
        Assert.Equal(1, await ReadSchemaVersionAsync());

        await repository.SearchAsync();

        Assert.Equal(1, await ReadSchemaVersionAsync());
    }

    public void Dispose()
    {
        if (File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }
    }

    private async Task<HashSet<string>> ReadUserTableNamesAsync()
    {
        await using SqliteConnection connection = new(ConnectionString);
        await connection.OpenAsync();

        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT name
            FROM sqlite_master
            WHERE type = 'table'
              AND name NOT LIKE 'sqlite_%';
            """;

        await using SqliteDataReader reader = await command.ExecuteReaderAsync();
        HashSet<string> tableNames = new(StringComparer.OrdinalIgnoreCase);

        while (await reader.ReadAsync())
        {
            tableNames.Add(reader.GetString(0));
        }

        return tableNames;
    }

    private async Task<int> ReadSchemaVersionAsync()
    {
        await using SqliteConnection connection = new(ConnectionString);
        await connection.OpenAsync();

        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";

        object? value = await command.ExecuteScalarAsync();

        return Convert.ToInt32(value);
    }
}