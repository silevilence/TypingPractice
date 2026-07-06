using TypingCore.Abstractions;
using TypingCore.Models;
using TypingCore.Parsing;
using TypingCore.Wpf.Services;
using TypingCore.Wpf.ViewModels;

namespace TypingCore.Tests.Wpf;

public sealed class PhaseEightTypingInputTests
{
    private const int WmKeyDown = 0x0100;
    private const int WmImeChar = 0x0286;
    private const int VkA = 0x41;
    private const int VkBackspace = 0x08;
    private const int VkEnter = 0x0D;
    private const int VkEscape = 0x1B;
    private const int VkLeftArrow = 0x25;

    [Fact]
    public async Task MainViewModel_starts_typing_practice_from_article_card()
    {
        MutableSystemClock clock = new(new DateTimeOffset(2026, 7, 3, 12, 0, 0, TimeSpan.Zero));
        FakeArticleRepository repository = new(
            new Article(
                "article-1",
                "春晓",
                "春眠不觉晓",
                clock.UtcNow,
                new[] { "古诗" }));
        InMemorySessionRepository sessionRepository = new();
        ArticleLibraryViewModel articleLibrary = CreateArticleLibraryViewModel(repository, clock);
        MainViewModel viewModel = new(
            articleLibrary,
            new HistoryViewModel(repository, sessionRepository),
            new SettingsViewModel(),
            new ArticleTextLayoutBuilder(),
            clock,
            sessionRepository);

        await articleLibrary.LoadAsync();

        ArticleCardViewModel articleCard = Assert.Single(articleLibrary.Articles);
        articleCard.StartPracticeCommand.Execute(null);

        TypingPracticeViewModel practice = Assert.IsType<TypingPracticeViewModel>(viewModel.CurrentPage);
        Assert.Equal("春晓", practice.ArticleTitle);
        Assert.Equal("春眠不觉晓", practice.TargetText);
        Assert.Equal(0, practice.CurrentTextIndex);
    }

    [Fact]
    public void TypingPracticeViewModel_routes_wm_keydown_and_textinput_into_session()
    {
        MutableSystemClock clock = new(new DateTimeOffset(2026, 7, 3, 12, 30, 0, TimeSpan.Zero));
        TypingPracticeViewModel viewModel = CreatePracticeViewModel("ab", clock);

        bool handledKeyDown = viewModel.HandleWindowMessage(WmKeyDown, (nint)VkA);
        clock.Advance(TimeSpan.FromMilliseconds(5));
        bool handledTextInput = viewModel.HandleTextInput("a");

        Assert.True(handledKeyDown);
        Assert.True(handledTextInput);
        Assert.Equal("a", viewModel.CommittedText);
        Assert.Equal(1, viewModel.CurrentTextIndex);
        Assert.Equal(TypingSessionState.Running, viewModel.SessionState);
    }

    [Fact]
    public void TypingPracticeViewModel_suppresses_duplicate_textinput_after_wm_ime_char()
    {
        MutableSystemClock clock = new(new DateTimeOffset(2026, 7, 3, 13, 0, 0, TimeSpan.Zero));
        TypingPracticeViewModel viewModel = CreatePracticeViewModel("你好", clock);

        bool handledImeCommit = viewModel.HandleWindowMessage(WmImeChar, (nint)'你');
        clock.Advance(TimeSpan.FromMilliseconds(10));
        bool handledDuplicateTextInput = viewModel.HandleTextInput("你");

        Assert.True(handledImeCommit);
        Assert.False(handledDuplicateTextInput);
        Assert.Equal("你", viewModel.CommittedText);
        Assert.Equal(1, viewModel.CurrentTextIndex);
        Assert.False(viewModel.IsCompleted);
    }

    [Fact]
    public void TypingPracticeViewModel_uses_special_keys_and_requires_explicit_restart_confirmation()
    {
        MutableSystemClock clock = new(new DateTimeOffset(2026, 7, 3, 13, 30, 0, TimeSpan.Zero));
        TypingPracticeViewModel viewModel = CreatePracticeViewModel("ab", clock);

        viewModel.HandleWindowMessage(WmKeyDown, (nint)VkA);
        clock.Advance(TimeSpan.FromMilliseconds(5));
        viewModel.HandleTextInput("a");

        clock.Advance(TimeSpan.FromMilliseconds(5));
        bool handledBackspace = viewModel.HandleWindowMessage(WmKeyDown, (nint)VkBackspace);

        Assert.True(handledBackspace);
        Assert.Equal(string.Empty, viewModel.CommittedText);
        Assert.Equal(0, viewModel.CurrentTextIndex);

        clock.Advance(TimeSpan.FromMilliseconds(5));
        bool handledArrow = viewModel.HandleWindowMessage(WmKeyDown, (nint)VkLeftArrow);

        Assert.True(handledArrow);
        Assert.Contains("方向键", viewModel.StatusMessage);

        viewModel.HandleWindowMessage(WmKeyDown, (nint)VkA);
        clock.Advance(TimeSpan.FromMilliseconds(5));
        viewModel.HandleTextInput("a");

        clock.Advance(TimeSpan.FromMilliseconds(5));
        bool handledEscape = viewModel.HandleWindowMessage(WmKeyDown, (nint)VkEscape);

        Assert.True(handledEscape);
        Assert.True(viewModel.IsRestartConfirmationVisible);
        Assert.Equal("a", viewModel.CommittedText);
        Assert.Equal(1, viewModel.CurrentTextIndex);

        bool handledRepeatedEscape = viewModel.HandleWindowMessage(WmKeyDown, (nint)VkEscape);

        Assert.True(handledRepeatedEscape);
        Assert.False(viewModel.IsRestartConfirmationVisible);
        Assert.Equal(1, viewModel.CurrentTextIndex);

        viewModel.RestartCommand.Execute(null);
        Assert.True(viewModel.IsRestartConfirmationVisible);

        bool ignoredText = viewModel.HandleTextInput("b");
        Assert.True(ignoredText);
        Assert.Equal(1, viewModel.CurrentTextIndex);

        viewModel.ConfirmRestartCommand.Execute(null);

        Assert.False(viewModel.IsRestartConfirmationVisible);
        Assert.Equal(string.Empty, viewModel.CommittedText);
        Assert.Equal(0, viewModel.CurrentTextIndex);
        Assert.Contains("重新开始", viewModel.StatusMessage);
    }

    [Fact]
    public void TypingPracticeViewModel_prompts_for_enter_at_line_break_and_advances_to_next_line()
    {
        MutableSystemClock clock = new(new DateTimeOffset(2026, 7, 3, 14, 0, 0, TimeSpan.Zero));
        TypingPracticeViewModel viewModel = CreatePracticeViewModel("甲\n乙", clock);

        bool handledFirstCharacter = viewModel.HandleTextInput("甲");

        Assert.True(handledFirstCharacter);
        Assert.Equal(1, viewModel.CurrentTextIndex);
        Assert.Contains("Enter", viewModel.StatusMessage);

        clock.Advance(TimeSpan.FromMilliseconds(5));
        bool handledLineBreak = viewModel.HandleTextInput("\n");

        Assert.True(handledLineBreak);
        Assert.Equal(2, viewModel.CurrentTextIndex);
        Assert.DoesNotContain("Enter", viewModel.StatusMessage);
    }

    [Fact]
    public void TypingPracticeViewModel_routes_enter_preview_key_into_line_break_commit()
    {
        MutableSystemClock clock = new(new DateTimeOffset(2026, 7, 3, 14, 15, 0, TimeSpan.Zero));
        TypingPracticeViewModel viewModel = CreatePracticeViewModel("甲\n乙", clock);

        viewModel.HandleTextInput("甲");
        clock.Advance(TimeSpan.FromMilliseconds(5));

        bool handledPreviewKey = viewModel.HandlePreviewKeyDown(VkEnter);

        Assert.True(handledPreviewKey);
        Assert.Equal(2, viewModel.CurrentTextIndex);
        Assert.Contains("下一行", viewModel.StatusMessage);
    }

    [Fact]
    public void TypingPracticeViewModel_routes_backspace_preview_key_into_session()
    {
        MutableSystemClock clock = new(new DateTimeOffset(2026, 7, 3, 14, 30, 0, TimeSpan.Zero));
        TypingPracticeViewModel viewModel = CreatePracticeViewModel("ab", clock);

        viewModel.HandleTextInput("a");
        clock.Advance(TimeSpan.FromMilliseconds(5));

        bool handledPreviewKey = viewModel.HandlePreviewKeyDown(VkBackspace);

        Assert.True(handledPreviewKey);
        Assert.Equal(0, viewModel.CurrentTextIndex);
        Assert.Equal(string.Empty, viewModel.CommittedText);
    }

    private static ArticleLibraryViewModel CreateArticleLibraryViewModel(
        FakeArticleRepository repository,
        MutableSystemClock clock)
        => new(
            repository,
            new FakeArticleImportService(),
            new FakeFileDialogService(),
            new FakeClipboardService(),
            clock);

    private static TypingPracticeViewModel CreatePracticeViewModel(string rawText, MutableSystemClock clock)
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

    private sealed class MutableSystemClock(DateTimeOffset utcNow) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; private set; } = utcNow;

        public void Advance(TimeSpan duration) => UtcNow = UtcNow.Add(duration);
    }

    private sealed class FakeArticleRepository(params Article[] initialArticles) : IArticleRepository
    {
        private readonly List<Article> storedArticles = initialArticles.ToList();

        public Task SaveAsync(IArticleRecord articleRecord, CancellationToken cancellationToken = default)
        {
            storedArticles.RemoveAll(article => article.ArticleId == articleRecord.ArticleId);
            storedArticles.Add(new Article(
                articleRecord.ArticleId,
                articleRecord.Title,
                articleRecord.RawText,
                articleRecord.CreatedAt,
                articleRecord.Tags));
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string articleId, CancellationToken cancellationToken = default)
        {
            storedArticles.RemoveAll(article => article.ArticleId == articleId);
            return Task.CompletedTask;
        }

        public Task<IArticleRecord?> GetByIdAsync(string articleId, CancellationToken cancellationToken = default)
            => Task.FromResult<IArticleRecord?>(storedArticles.SingleOrDefault(article => article.ArticleId == articleId));

        public Task<IReadOnlyList<IArticleRecord>> SearchAsync(
            string? query = null,
            string? tag = null,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<Article> results = storedArticles;

            if (!string.IsNullOrWhiteSpace(query))
            {
                results = results.Where(article => article.Title.Contains(query, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(tag))
            {
                results = results.Where(article => article.Tags.Contains(tag));
            }

            return Task.FromResult<IReadOnlyList<IArticleRecord>>(
                results
                    .OrderByDescending(article => article.CreatedAt)
                    .Cast<IArticleRecord>()
                    .ToArray());
        }
    }

    private sealed class FakeArticleImportService : IArticleImportService
    {
        public ArticleImportResult ImportFromText(string title, string rawText)
            => new(title, new ArticleTextLayoutBuilder().Build(rawText), detectedEncodingName: null);

        public Task<ArticleImportResult> ImportFromFileAsync(string filePath, CancellationToken cancellationToken = default)
            => Task.FromResult(ImportFromText(Path.GetFileNameWithoutExtension(filePath), string.Empty));
    }

    private sealed class FakeFileDialogService : IFileDialogService
    {
        public string? SelectArticleFile() => null;

        public string? SelectCodeTableFile() => null;
    }

    private sealed class FakeClipboardService : IClipboardService
    {
        public string? ReadText() => null;
    }
}
