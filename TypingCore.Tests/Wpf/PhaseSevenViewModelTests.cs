using TypingCore.Abstractions;
using TypingCore.Models;
using TypingCore.Parsing;
using TypingCore.Wpf.Services;
using TypingCore.Wpf.ViewModels;

namespace TypingCore.Tests.Wpf;

public sealed class PhaseSevenViewModelTests
{
    [Fact]
    public void MainViewModel_defaults_to_article_library_and_switches_pages()
    {
        ArticleLibraryViewModel articleLibrary = CreateArticleLibraryViewModel();
        SettingsViewModel settings = new();

        MainViewModel viewModel = new(articleLibrary, settings);

        Assert.Same(articleLibrary, viewModel.CurrentPage);
        Assert.True(viewModel.IsArticleLibrarySelected);
        Assert.False(viewModel.IsSettingsSelected);

        viewModel.ShowSettingsCommand.Execute(null);

        Assert.Same(settings, viewModel.CurrentPage);
        Assert.False(viewModel.IsArticleLibrarySelected);
        Assert.True(viewModel.IsSettingsSelected);

        viewModel.ShowArticleLibraryCommand.Execute(null);

        Assert.Same(articleLibrary, viewModel.CurrentPage);
        Assert.True(viewModel.IsArticleLibrarySelected);
        Assert.False(viewModel.IsSettingsSelected);
    }

    [Fact]
    public async Task LoadAsync_populates_article_cards_and_tag_filters()
    {
        FakeArticleRepository repository = new(
            new Article(
                "article-1",
                "五笔入门",
                "春眠不觉晓",
                new DateTimeOffset(2026, 7, 3, 8, 0, 0, TimeSpan.Zero),
                new[] { "古诗", "五笔" }),
            new Article(
                "article-2",
                "速度练习",
                "夜来风雨声",
                new DateTimeOffset(2026, 7, 3, 9, 0, 0, TimeSpan.Zero),
                new[] { "练习" }));

        ArticleLibraryViewModel viewModel = CreateArticleLibraryViewModel(repository);

        await viewModel.LoadAsync();

        Assert.Equal(new[] { "速度练习", "五笔入门" }, viewModel.Articles.Select(article => article.Title));
        Assert.Equal(
            new[] { ArticleLibraryViewModel.AllTagsOption, "五笔", "古诗", "练习" },
            viewModel.AvailableTags);
        Assert.Equal(ArticleLibraryViewModel.AllTagsOption, viewModel.SelectedTag);
    }

    [Fact]
    public async Task SearchAsync_uses_trimmed_query_and_selected_tag()
    {
        FakeArticleRepository repository = new(
            new Article(
                "article-1",
                "五笔入门",
                "春眠不觉晓",
                new DateTimeOffset(2026, 7, 3, 8, 0, 0, TimeSpan.Zero),
                new[] { "古诗", "五笔" }));

        ArticleLibraryViewModel viewModel = CreateArticleLibraryViewModel(repository);
        viewModel.SearchText = "  五笔  ";
        viewModel.SelectedTag = "古诗";

        await viewModel.SearchAsync();

        Assert.Equal("五笔", repository.LastQuery);
        Assert.Equal("古诗", repository.LastTag);
        Assert.Single(viewModel.Articles);
    }

    [Fact]
    public async Task ClearFiltersAsync_resets_inputs_and_restores_full_results()
    {
        FakeArticleRepository repository = new(
            new Article(
                "article-1",
                "五笔入门",
                "春眠不觉晓",
                new DateTimeOffset(2026, 7, 3, 8, 0, 0, TimeSpan.Zero),
                new[] { "古诗", "五笔" }),
            new Article(
                "article-2",
                "速度练习",
                "夜来风雨声",
                new DateTimeOffset(2026, 7, 3, 9, 0, 0, TimeSpan.Zero),
                new[] { "练习" }));

        ArticleLibraryViewModel viewModel = CreateArticleLibraryViewModel(repository);
        await viewModel.LoadAsync();

        viewModel.SearchText = "五笔";
        viewModel.SelectedTag = "古诗";
        await viewModel.SearchAsync();

        Assert.Single(viewModel.Articles);

        await viewModel.ClearFiltersAsync();

        Assert.Equal(string.Empty, viewModel.SearchText);
        Assert.Equal(ArticleLibraryViewModel.AllTagsOption, viewModel.SelectedTag);
        Assert.Equal(2, viewModel.Articles.Count);
    }

    [Fact]
    public async Task ImportFromClipboardAsync_saves_normalized_article_and_refreshes_results()
    {
        FakeArticleRepository repository = new();
        FakeArticleImportService importService = new();
        FakeClipboardService clipboardService = new() { Text = "春眠不觉晓" };
        FakeSystemClock clock = new(new DateTimeOffset(2026, 7, 3, 10, 30, 0, TimeSpan.Zero));

        ArticleLibraryViewModel viewModel = CreateArticleLibraryViewModel(
            repository,
            importService,
            clipboardService: clipboardService,
            clock: clock);
        viewModel.ClipboardTitle = "古诗练习";
        viewModel.ImportTagsText = "古诗， 五笔, 古诗";

        await viewModel.ImportFromClipboardAsync();

        Article saved = Assert.Single(repository.StoredArticles);
        Assert.Equal("古诗练习", saved.Title);
        Assert.Equal("春眠不觉晓", saved.RawText);
        Assert.Equal(new[] { "古诗", "五笔" }, saved.Tags);
        Assert.Equal(clock.UtcNow, saved.CreatedAt);
        Assert.Equal(("古诗练习", "春眠不觉晓"), importService.LastTextImport);
        Assert.Single(viewModel.Articles);
        Assert.Contains("古诗练习", viewModel.StatusMessage);
    }

    [Fact]
    public async Task ImportFromFileAsync_uses_file_dialog_and_import_result_title()
    {
        FakeArticleRepository repository = new();
        FakeArticleImportService importService = new()
        {
            FileImportResult = CreateImportResult("文件导入", "夜来风雨声"),
        };
        FakeFileDialogService fileDialogService = new() { FilePath = @"C:\temp\article.txt" };

        ArticleLibraryViewModel viewModel = CreateArticleLibraryViewModel(
            repository,
            importService,
            fileDialogService: fileDialogService);
        viewModel.ImportTagsText = "练习";

        await viewModel.ImportFromFileAsync();

        Article saved = Assert.Single(repository.StoredArticles);
        Assert.Equal(@"C:\temp\article.txt", importService.LastFilePath);
        Assert.Equal("文件导入", saved.Title);
        Assert.Equal("夜来风雨声", saved.RawText);
        Assert.Equal(new[] { "练习" }, saved.Tags);
    }

    [Fact]
    public void SettingsViewModel_exposes_stage_seven_scaffold_options()
    {
        SettingsViewModel viewModel = new();

        Assert.Contains("跟随系统", viewModel.ThemeOptions);
        Assert.Contains("霞鹜文楷", viewModel.FontOptions);
        Assert.Contains(viewModel.ShortcutHints, hint => hint.Contains("暂停"));
        Assert.NotEmpty(viewModel.Description);
    }

    private static ArticleLibraryViewModel CreateArticleLibraryViewModel(
        FakeArticleRepository? repository = null,
        FakeArticleImportService? importService = null,
        FakeFileDialogService? fileDialogService = null,
        FakeClipboardService? clipboardService = null,
        FakeSystemClock? clock = null)
        => new(
            repository ?? new FakeArticleRepository(),
            importService ?? new FakeArticleImportService(),
            fileDialogService ?? new FakeFileDialogService(),
            clipboardService ?? new FakeClipboardService(),
            clock ?? new FakeSystemClock(new DateTimeOffset(2026, 7, 3, 8, 0, 0, TimeSpan.Zero)));

    private static ArticleImportResult CreateImportResult(string title, string rawText)
        => new(title, new ArticleTextLayoutBuilder().Build(rawText), detectedEncodingName: null);

    private sealed class FakeArticleRepository(params Article[] initialArticles) : IArticleRepository
    {
        private readonly List<Article> storedArticles = initialArticles.ToList();

        public IReadOnlyList<Article> StoredArticles => storedArticles;

        public string? LastQuery { get; private set; }

        public string? LastTag { get; private set; }

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
            LastQuery = query;
            LastTag = tag;

            IEnumerable<Article> results = storedArticles;

            if (!string.IsNullOrWhiteSpace(query))
            {
                results = results.Where(article =>
                    article.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    article.RawText.Contains(query, StringComparison.OrdinalIgnoreCase));
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
        public ArticleImportResult FileImportResult { get; set; } = CreateImportResult("默认标题", "默认内容");

        public (string Title, string Text)? LastTextImport { get; private set; }

        public string? LastFilePath { get; private set; }

        public ArticleImportResult ImportFromText(string title, string rawText)
        {
            LastTextImport = (title, rawText);
            return CreateImportResult(title, rawText);
        }

        public Task<ArticleImportResult> ImportFromFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            LastFilePath = filePath;
            return Task.FromResult(FileImportResult);
        }
    }

    private sealed class FakeFileDialogService : IFileDialogService
    {
        public string? FilePath { get; set; }

        public string? SelectArticleFile() => FilePath;
    }

    private sealed class FakeClipboardService : IClipboardService
    {
        public string? Text { get; set; }

        public string? ReadText() => Text;
    }

    private sealed class FakeSystemClock(DateTimeOffset utcNow) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}