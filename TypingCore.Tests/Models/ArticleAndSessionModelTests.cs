using TypingCore.Abstractions;
using TypingCore.Models;

namespace TypingCore.Tests.Models;

public class ArticleAndSessionModelTests
{
    [Fact]
    public void Article_implements_article_record_contract()
    {
        DateTimeOffset createdAt = new(2026, 7, 3, 8, 0, 0, TimeSpan.Zero);

        Article article = new(
            "article-1",
            "示例文章",
            "中A",
            createdAt,
            new[] { "练习", "阶段二" });

        IArticleRecord record = article;

        Assert.Equal("article-1", record.ArticleId);
        Assert.Equal("示例文章", record.Title);
        Assert.Equal("中A", record.RawText);
        Assert.Equal(createdAt, record.CreatedAt);
        Assert.Equal(new[] { "练习", "阶段二" }, record.Tags);
    }

    [Fact]
    public void TypingSessionRecord_implements_session_record_contract()
    {
        DateTimeOffset startedAt = new(2026, 7, 3, 8, 30, 0, TimeSpan.Zero);
        DateTimeOffset endedAt = startedAt.AddMinutes(3);
        SessionStatistics statistics = new(
            320,
            260,
            52,
            4.1,
            3,
            0.1,
            0.04,
            TimeSpan.FromMinutes(3));

        TypingSessionRecord session = new(
            "session-1",
            "article-1",
            startedAt,
            endedAt,
            statistics);

        ISessionRecord record = session;

        Assert.Equal("session-1", record.SessionId);
        Assert.Equal("article-1", record.ArticleId);
        Assert.Equal(startedAt, record.StartedAt);
        Assert.Equal(endedAt, record.EndedAt);
        Assert.NotNull(record.Statistics);
        Assert.Equal(320, record.Statistics!.KeystrokesPerMinute);
        Assert.Equal(TimeSpan.FromMinutes(3), record.Statistics.Elapsed);
    }
}