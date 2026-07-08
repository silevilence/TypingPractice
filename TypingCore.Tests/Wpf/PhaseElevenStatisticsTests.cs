using LiveChartsCore.SkiaSharpView;
using TypingCore.Abstractions;
using TypingCore.Models;
using TypingCore.Parsing;
using TypingCore.Wpf.Services;
using TypingCore.Wpf.ViewModels;

namespace TypingCore.Tests.Wpf;

public sealed class PhaseElevenStatisticsTests
{
    private const int WmKeyDown = 0x0100;
    private const int VkA = 0x41;
    private const int VkB = 0x42;
    private const int VkX = 0x58;

    [Fact]
    public void HandleTextInput_updates_realtime_statistics_after_normalized_input()
    {
        MutableSystemClock clock = new(new DateTimeOffset(2026, 7, 6, 12, 0, 0, TimeSpan.Zero));
        TypingPracticeViewModel viewModel = CreatePracticeViewModel("ab", clock);

        viewModel.HandleWindowMessage(WmKeyDown, (nint)VkA);
        viewModel.HandleTextInput("a");
        clock.Advance(TimeSpan.FromSeconds(1));
        viewModel.HandleWindowMessage(WmKeyDown, (nint)VkB);
        viewModel.HandleTextInput("b");

        Assert.Equal(120d, viewModel.KeystrokesPerMinute);
        Assert.Equal(120d, viewModel.CharactersPerMinute);
        Assert.Equal(1d, viewModel.AverageCodeLength);
        Assert.Equal(0, viewModel.ErrorCharacterCount);
    }

    [Fact]
    public async Task CompletionTask_saves_session_and_builds_result_summary()
    {
        MutableSystemClock clock = new(new DateTimeOffset(2026, 7, 6, 13, 0, 0, TimeSpan.Zero));
        InMemorySessionRepository repository = new();
        PracticeResultViewModel? result = null;
        Article article = new(
            "article-1",
            "练习文章",
            "ab",
            clock.UtcNow,
            Array.Empty<string>());
        TypingPracticeViewModel viewModel = new(
            article,
            new ArticleTextLayoutBuilder(),
            clock,
            () => { },
            repository,
            (completedArticle, statistics, errorCount) =>
            {
                result = new PracticeResultViewModel(
                    completedArticle,
                    statistics,
                    errorCount,
                    () => { },
                    () => Task.CompletedTask,
                    () => { });
            });

        viewModel.HandleWindowMessage(WmKeyDown, (nint)VkA);
        viewModel.HandleTextInput("a");
        clock.Advance(TimeSpan.FromSeconds(1));
        viewModel.HandleWindowMessage(WmKeyDown, (nint)VkX);
        viewModel.HandleTextInput("x");
        await viewModel.CompletionTask;

        ISessionRecord saved = Assert.Single(repository.Sessions);
        Assert.Equal("article-1", saved.ArticleId);
        Assert.NotNull(saved.EndedAt);
        Assert.NotNull(saved.Statistics);

        Assert.NotNull(result);
        Assert.Equal("练习文章", result!.ArticleTitle);
        Assert.Equal(1, result.ErrorCharacterCount);
        Assert.Equal(0.5, result.ErrorRate);
        Assert.Equal(120d, result.CharactersPerMinute);
    }

    [Fact]
    public async Task LoadAsync_filters_completed_sessions_and_builds_article_trends()
    {
        DateTimeOffset startedAt = new(2026, 7, 6, 14, 0, 0, TimeSpan.FromHours(8));
        InMemoryArticleRepository articleRepository = new(
            new Article("article-1", "春晓", "春眠不觉晓", startedAt, Array.Empty<string>()),
            new Article("article-2", "静夜思", "床前明月光", startedAt, Array.Empty<string>()));
        InMemorySessionRepository sessionRepository = new(
            CreateSession("session-2", "article-1", startedAt.AddDays(1), 180, 0.02),
            CreateSession("session-1", "article-1", startedAt, 120, 0.05),
            new TypingSessionRecord("running", "article-1", startedAt.AddDays(2), null),
            CreateSession("other", "article-2", startedAt, 240, 0.01));
        HistoryViewModel viewModel = new(articleRepository, sessionRepository);

        await viewModel.LoadAsync("article-1");

        Assert.Equal("春晓", viewModel.SelectedArticleTitle);
        Assert.Equal(2, viewModel.Records.Count);
        Assert.Equal(180d, viewModel.Records[0].CharactersPerMinute);
        Assert.Single(viewModel.SpeedSeries);
        Assert.Single(viewModel.ErrorRateSeries);

        LineSeries<double> speedSeries = Assert.IsType<LineSeries<double>>(viewModel.SpeedSeries[0]);
        Assert.Equal(new[] { 120d, 180d }, speedSeries.Values);
        Assert.Equal(new[] { "07-06 14:00", "07-07 14:00" }, viewModel.SpeedXAxes[0].Labels);
    }

    private static TypingPracticeViewModel CreatePracticeViewModel(
        string rawText,
        MutableSystemClock clock)
        => new(
            new Article(
                "article-1",
                "练习文章",
                rawText,
                clock.UtcNow,
                Array.Empty<string>()),
            new ArticleTextLayoutBuilder(),
            clock,
            () => { });

    private static TypingSessionRecord CreateSession(
        string sessionId,
        string articleId,
        DateTimeOffset startedAt,
        double charactersPerMinute,
        double errorRate)
        => new(
            sessionId,
            articleId,
            startedAt,
            startedAt.AddMinutes(1),
            new SessionStatistics(
                charactersPerMinute * 1.2,
                charactersPerMinute,
                charactersPerMinute / 5,
                1.2,
                1,
                0.01,
                errorRate,
                TimeSpan.FromMinutes(1)));

    private sealed class MutableSystemClock(DateTimeOffset utcNow) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; private set; } = utcNow;

        public void Advance(TimeSpan duration) => UtcNow = UtcNow.Add(duration);
    }

    private sealed class InMemoryArticleRepository(params Article[] articles) : IArticleRepository
    {
        public Task SaveAsync(IArticleRecord articleRecord, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DeleteAsync(string articleId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RestoreAsync(string articleId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IArticleRecord?> GetByIdAsync(
            string articleId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IArticleRecord?>(
                articles.SingleOrDefault(article => article.ArticleId == articleId));

        public Task<IReadOnlyList<IArticleRecord>> SearchAsync(
            string? query = null,
            string? tag = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<IArticleRecord>>(
                articles.Cast<IArticleRecord>().ToArray());
    }
}

internal sealed class InMemorySessionRepository(params ISessionRecord[] sessions) : ISessionRepository
{
    private readonly List<ISessionRecord> storedSessions = sessions.ToList();

    public IReadOnlyList<ISessionRecord> Sessions => storedSessions;

    public Task SaveAsync(ISessionRecord sessionRecord, CancellationToken cancellationToken = default)
    {
        storedSessions.RemoveAll(session => session.SessionId == sessionRecord.SessionId);
        storedSessions.Add(sessionRecord);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ISessionRecord>> GetByArticleIdAsync(
        string articleId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ISessionRecord>>(
            storedSessions
                .Where(session => session.ArticleId == articleId)
                .OrderByDescending(session => session.StartedAt)
                .ToArray());

    public Task<IReadOnlyList<ISessionRecord>> GetByTimeRangeAsync(
        DateTimeOffset startInclusive,
        DateTimeOffset endExclusive,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ISessionRecord>>(
            storedSessions
                .Where(session => session.StartedAt >= startInclusive && session.StartedAt < endExclusive)
                .OrderBy(session => session.StartedAt)
                .ToArray());
}
