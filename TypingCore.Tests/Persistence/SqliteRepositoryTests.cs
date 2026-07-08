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
    public async Task Article_repository_supports_save_search_update_soft_delete_and_restore()
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
        Assert.Empty(await repository.SearchAsync(tag: "双拼"));

        await repository.RestoreAsync("article-1");

        Assert.NotNull(await repository.GetByIdAsync("article-1"));
        Assert.Single(await repository.SearchAsync(tag: "双拼"));
    }

    [Fact]
    public async Task Article_repository_tag_methods_update_related_articles()
    {
        IArticleRepository repository = new SqliteArticleRepository(ConnectionString);
        DateTimeOffset createdAt = new(2026, 7, 3, 9, 0, 0, TimeSpan.Zero);

        await repository.SaveAsync(new Article(
            "article-1",
            "五笔入门",
            "春眠不觉晓",
            createdAt,
            new[] { "古诗" }));
        await repository.SaveAsync(new Article(
            "article-2",
            "速度练习",
            "夜来风雨声",
            createdAt.AddMinutes(1),
            new[] { "古诗", "练习" }));

        await repository.AddTagAsync("article-1", "五笔");
        await repository.RenameTagAsync("古诗", "诗词");
        await repository.DeleteTagAsync("练习");

        Assert.Equal(new[] { "五笔", "诗词" }, await repository.GetTagsAsync());
        Assert.Equal(new[] { "诗词", "五笔" }, (await repository.GetByIdAsync("article-1"))!.Tags);
        Assert.Equal(new[] { "诗词" }, (await repository.GetByIdAsync("article-2"))!.Tags);
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
            0.05,
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
        Assert.Equal(0.05, byArticle.Statistics.BackspaceRate);
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
        HashSet<string> articleColumns = await ReadTableColumnNamesAsync("articles");
        HashSet<string> sessionColumns = await ReadTableColumnNamesAsync("sessions");

        Assert.Contains("articles", tableNames);
        Assert.Contains("sessions", tableNames);
        Assert.Contains("keystrokes", tableNames);
        Assert.Contains("codetables", tableNames);
        Assert.Contains("is_deleted", articleColumns);
        Assert.Contains("backspace_rate", sessionColumns);
        Assert.Equal(3, await ReadSchemaVersionAsync());

        await repository.SearchAsync();

        Assert.Equal(3, await ReadSchemaVersionAsync());
    }

    [Fact]
    public async Task Session_repository_migrates_version_1_database_to_current_schema()
    {
        await InitializeVersion1DatabaseAsync();

        ISessionRepository repository = new SqliteSessionRepository(ConnectionString);
        DateTimeOffset startedAt = new(2026, 7, 3, 13, 0, 0, TimeSpan.Zero);
        SessionStatistics statistics = new(
            300,
            240,
            48,
            4,
            1,
            0.2,
            0.01,
            TimeSpan.FromMinutes(1));

        await repository.SaveAsync(new TypingSessionRecord(
            "session-migrated",
            "article-legacy",
            startedAt,
            startedAt.AddMinutes(1),
            statistics));

        ISessionRecord migrated = Assert.Single(await repository.GetByArticleIdAsync("article-legacy"));

        Assert.NotNull(migrated.Statistics);
        Assert.Equal(0.2, migrated.Statistics!.BackspaceRate);
        Assert.Equal(3, await ReadSchemaVersionAsync());
        Assert.Contains("backspace_rate", await ReadTableColumnNamesAsync("sessions"));
        Assert.Contains("is_deleted", await ReadTableColumnNamesAsync("articles"));
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

    private async Task<HashSet<string>> ReadTableColumnNamesAsync(string tableName)
    {
        await using SqliteConnection connection = new(ConnectionString);
        await connection.OpenAsync();

        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({tableName});";

        await using SqliteDataReader reader = await command.ExecuteReaderAsync();
        HashSet<string> columnNames = new(StringComparer.OrdinalIgnoreCase);

        while (await reader.ReadAsync())
        {
            columnNames.Add(reader.GetString(reader.GetOrdinal("name")));
        }

        return columnNames;
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

    private async Task InitializeVersion1DatabaseAsync()
    {
        await using SqliteConnection connection = new(ConnectionString);
        await connection.OpenAsync();

        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version = 1;";
        await command.ExecuteNonQueryAsync();

        command.CommandText = """
            CREATE TABLE articles (
                article_id TEXT PRIMARY KEY,
                title TEXT NOT NULL,
                raw_text TEXT NOT NULL,
                created_at TEXT NOT NULL,
                tags_json TEXT NOT NULL
            );

            CREATE TABLE sessions (
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
            """;
        await command.ExecuteNonQueryAsync();
    }
}
