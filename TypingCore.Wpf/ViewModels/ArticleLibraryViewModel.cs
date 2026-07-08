using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TypingCore.Abstractions;
using TypingCore.Models;
using TypingCore.Wpf.Services;

namespace TypingCore.Wpf.ViewModels;

/// <summary>
/// Provides the article library page behavior for phase seven.
/// </summary>
public sealed class ArticleLibraryViewModel : PageViewModel
{
    public const string AllTagsOption = "全部标签";
    public const string SortByCreatedDescOption = "创建时间 ↓";
    public const string SortByCreatedAscOption = "创建时间 ↑";
    public const string SortByLengthDescOption = "字数 ↓";
    public const string SortByLengthAscOption = "字数 ↑";

    private readonly IArticleRepository articleRepository;
    private readonly IArticleImportService articleImportService;
    private readonly IFileDialogService fileDialogService;
    private readonly IClipboardService clipboardService;
    private readonly ISystemClock systemClock;
    private string searchText = string.Empty;
    private string selectedTag = AllTagsOption;
    private string selectedSortOption = SortByCreatedDescOption;
    private string clipboardTitle = "剪贴板导入";
    private string importTagsText = string.Empty;
    private ArticleCardViewModel? pendingDeleteArticle;
    private string statusMessage = "从本地文件或剪贴板导入文章后，就可以开始练习。";
    private bool isBusy;
    private bool isDeleteConfirmationVisible;

    public ArticleLibraryViewModel(
        IArticleRepository articleRepository,
        IArticleImportService articleImportService,
        IFileDialogService fileDialogService,
        IClipboardService clipboardService,
        ISystemClock systemClock)
        : base("文章库")
    {
        this.articleRepository = articleRepository ?? throw new ArgumentNullException(nameof(articleRepository));
        this.articleImportService = articleImportService ?? throw new ArgumentNullException(nameof(articleImportService));
        this.fileDialogService = fileDialogService ?? throw new ArgumentNullException(nameof(fileDialogService));
        this.clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        this.systemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));

        Articles = new ObservableCollection<ArticleCardViewModel>();
        AvailableTags = new ObservableCollection<string> { AllTagsOption };
        SortOptions = new ObservableCollection<string>
        {
            SortByCreatedDescOption,
            SortByCreatedAscOption,
            SortByLengthDescOption,
            SortByLengthAscOption,
        };

        LoadCommand = new AsyncRelayCommand(LoadAsync, () => !IsBusy);
        SearchCommand = new AsyncRelayCommand(SearchAsync, () => !IsBusy);
        ImportFromFileCommand = new AsyncRelayCommand(ImportFromFileAsync, () => !IsBusy);
        ImportFromClipboardCommand = new AsyncRelayCommand(ImportFromClipboardAsync, () => !IsBusy);
        ClearFiltersCommand = new AsyncRelayCommand(ClearFiltersAsync, () => !IsBusy);
        CancelDeleteCommand = new RelayCommand(CancelDelete);
        ConfirmDeleteCommand = new AsyncRelayCommand(ConfirmDeleteAsync, () => !IsBusy && PendingDeleteArticle is not null);
    }

    public event Action<IArticleRecord>? PracticeRequested;

    public event Action<IArticleRecord>? EditRequested;

    public event Action<IArticleRecord>? PreviewRequested;

    public ObservableCollection<ArticleCardViewModel> Articles { get; }

    public ObservableCollection<string> AvailableTags { get; }

    public ObservableCollection<string> SortOptions { get; }

    public IAsyncRelayCommand LoadCommand { get; }

    public IAsyncRelayCommand SearchCommand { get; }

    public IAsyncRelayCommand ImportFromFileCommand { get; }

    public IAsyncRelayCommand ImportFromClipboardCommand { get; }

    public IAsyncRelayCommand ClearFiltersCommand { get; }

    public IRelayCommand CancelDeleteCommand { get; }

    public IAsyncRelayCommand ConfirmDeleteCommand { get; }

    public string SearchText
    {
        get => searchText;
        set => SetProperty(ref searchText, value);
    }

    public string SelectedTag
    {
        get => selectedTag;
        set => SetProperty(ref selectedTag, string.IsNullOrWhiteSpace(value) ? AllTagsOption : value);
    }

    public string SelectedSortOption
    {
        get => selectedSortOption;
        set => SetProperty(ref selectedSortOption, string.IsNullOrWhiteSpace(value) ? SortByCreatedDescOption : value);
    }

    public string ClipboardTitle
    {
        get => clipboardTitle;
        set => SetProperty(ref clipboardTitle, value);
    }

    public string ImportTagsText
    {
        get => importTagsText;
        set => SetProperty(ref importTagsText, value);
    }

    public string StatusMessage
    {
        get => statusMessage;
        private set => SetProperty(ref statusMessage, value);
    }

    public bool IsDeleteConfirmationVisible
    {
        get => isDeleteConfirmationVisible;
        private set => SetProperty(ref isDeleteConfirmationVisible, value);
    }

    public ArticleCardViewModel? PendingDeleteArticle
    {
        get => pendingDeleteArticle;
        private set
        {
            if (SetProperty(ref pendingDeleteArticle, value))
            {
                OnPropertyChanged(nameof(PendingDeleteTitle));
                ConfirmDeleteCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string PendingDeleteTitle => PendingDeleteArticle?.Title ?? string.Empty;

    public bool IsBusy
    {
        get => isBusy;
        private set
        {
            if (SetProperty(ref isBusy, value))
            {
                LoadCommand.NotifyCanExecuteChanged();
                SearchCommand.NotifyCanExecuteChanged();
                ImportFromFileCommand.NotifyCanExecuteChanged();
                ImportFromClipboardCommand.NotifyCanExecuteChanged();
                ClearFiltersCommand.NotifyCanExecuteChanged();
                ConfirmDeleteCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public async Task LoadAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            await RefreshTagOptionsAsync().ConfigureAwait(false);
            await SearchInternalAsync().ConfigureAwait(false);
            if (Articles.Count > 0)
            {
                StatusMessage = $"已加载 {Articles.Count} 篇文章，可按标题或标签筛选。";
            }
        }).ConfigureAwait(false);
    }

    public async Task SearchAsync()
        => await ExecuteBusyAsync(SearchInternalAsync).ConfigureAwait(false);

    public async Task ImportFromClipboardAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            string? clipboardText = clipboardService.ReadText();
            if (string.IsNullOrWhiteSpace(clipboardText))
            {
                StatusMessage = "剪贴板里还没有可导入的文本。";
                return;
            }

            string title = string.IsNullOrWhiteSpace(ClipboardTitle)
                ? $"剪贴板导入 {systemClock.UtcNow.LocalDateTime:MM-dd HH:mm}"
                : ClipboardTitle.Trim();

            ArticleImportResult importResult = articleImportService.ImportFromText(title, clipboardText);
            Article article = CreateArticle(importResult.Title, importResult.NormalizedText);
            await articleRepository.SaveAsync(article).ConfigureAwait(false);

            await RefreshTagOptionsAsync().ConfigureAwait(false);
            await SearchInternalAsync().ConfigureAwait(false);

            StatusMessage = $"已从剪贴板导入《{article.Title}》。";
            ClipboardTitle = article.Title;
        }).ConfigureAwait(false);
    }

    public async Task ImportFromFileAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            string? filePath = fileDialogService.SelectArticleFile();
            if (string.IsNullOrWhiteSpace(filePath))
            {
                StatusMessage = "已取消文件导入。";
                return;
            }

            ArticleImportResult importResult = await articleImportService.ImportFromFileAsync(filePath).ConfigureAwait(false);
            Article article = CreateArticle(importResult.Title, importResult.NormalizedText);
            await articleRepository.SaveAsync(article).ConfigureAwait(false);

            await RefreshTagOptionsAsync().ConfigureAwait(false);
            await SearchInternalAsync().ConfigureAwait(false);

            StatusMessage = $"已导入文件《{article.Title}》。";
        }).ConfigureAwait(false);
    }

    public async Task ClearFiltersAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            SearchText = string.Empty;
            SelectedTag = AllTagsOption;
            SelectedSortOption = SortByCreatedDescOption;
            await SearchInternalAsync().ConfigureAwait(false);
            StatusMessage = Articles.Count == 0
                ? "筛选条件已清空。当前还没有文章。"
                : $"筛选条件已清空，已恢复 {Articles.Count} 篇文章。";
        }).ConfigureAwait(false);
    }

    private async Task SearchInternalAsync()
    {
        string? query = NormalizeQuery(SearchText);
        string? tag = NormalizeTag(SelectedTag);
        IReadOnlyList<IArticleRecord> results = await articleRepository.SearchAsync(query, tag).ConfigureAwait(false);

        ReplaceArticles(SortArticles(results));

        if (results.Count == 0)
        {
            StatusMessage = "当前筛选条件下还没有文章。";
            return;
        }

        StatusMessage = $"当前显示 {results.Count} 篇文章。";
    }

    private async Task RefreshTagOptionsAsync()
    {
        IReadOnlyList<IArticleRecord> allArticles = await articleRepository.SearchAsync().ConfigureAwait(false);
        string[] tags = allArticles
            .SelectMany(article => article.Tags)
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(tag => tag, StringComparer.Ordinal)
            .ToArray();

        AvailableTags.Clear();
        AvailableTags.Add(AllTagsOption);

        foreach (string tag in tags)
        {
            AvailableTags.Add(tag);
        }

        if (!AvailableTags.Contains(SelectedTag))
        {
            SelectedTag = AllTagsOption;
        }
    }

    private Article CreateArticle(string title, string rawText)
        => new(
            Guid.NewGuid().ToString("N"),
            title,
            rawText,
            systemClock.UtcNow,
            ParseTags(ImportTagsText));

    private void ReplaceArticles(IReadOnlyList<IArticleRecord> results)
    {
        Articles.Clear();

        foreach (IArticleRecord article in results)
        {
            Articles.Add(new ArticleCardViewModel(
                article.ArticleId,
                article.Title,
                BuildPreview(article.RawText),
                $"{article.RawText.Length} 字",
                article.CreatedAt.LocalDateTime.ToString("yyyy-MM-dd HH:mm"),
                article.Tags.ToArray(),
                new RelayCommand(() => OpenPractice(article)),
                new RelayCommand(() => OpenEdit(article)),
                new RelayCommand(() => OpenPreview(article)),
                new RelayCommand(() => RequestDelete(article)),
                new RelayCommand<string>(tag => ApplyTagFilter(tag))));
        }
    }

    private void OpenPractice(IArticleRecord article)
    {
        PracticeRequested?.Invoke(article);
        StatusMessage = $"已打开《{article.Title}》的练习页。";
    }

    private void OpenEdit(IArticleRecord article)
    {
        EditRequested?.Invoke(article);
        StatusMessage = $"正在编辑《{article.Title}》。";
    }

    private void OpenPreview(IArticleRecord article)
    {
        PreviewRequested?.Invoke(article);
        StatusMessage = $"正在预览《{article.Title}》。";
    }

    private void ApplyTagFilter(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return;
        }

        SelectedTag = tag;
        _ = SearchAsync();
    }

    private void RequestDelete(IArticleRecord article)
    {
        PendingDeleteArticle = Articles.SingleOrDefault(card =>
            string.Equals(card.ArticleId, article.ArticleId, StringComparison.Ordinal));
        IsDeleteConfirmationVisible = PendingDeleteArticle is not null;
        StatusMessage = IsDeleteConfirmationVisible
            ? $"请确认是否删除《{article.Title}》。"
            : "待删除文章已不在当前列表中。";
    }

    private void CancelDelete()
    {
        IsDeleteConfirmationVisible = false;
        PendingDeleteArticle = null;
        StatusMessage = "已取消删除。";
    }

    private async Task ConfirmDeleteAsync()
    {
        ArticleCardViewModel? article = PendingDeleteArticle;
        if (article is null)
        {
            return;
        }

        await ExecuteBusyAsync(async () =>
        {
            await articleRepository.DeleteAsync(article.ArticleId).ConfigureAwait(false);
            IsDeleteConfirmationVisible = false;
            PendingDeleteArticle = null;
            await RefreshTagOptionsAsync().ConfigureAwait(false);
            await SearchInternalAsync().ConfigureAwait(false);
            StatusMessage = $"已删除《{article.Title}》。";
        }).ConfigureAwait(false);
    }

    private async Task ExecuteBusyAsync(Func<Task> operation)
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            await operation().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            StatusMessage = $"操作没有完成：{ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string[] ParseTags(string rawTags)
        => rawTags
            .Split([',', '，', ';', '；', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static string? NormalizeQuery(string value)
    {
        string trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    private static string? NormalizeTag(string value)
        => string.Equals(value, AllTagsOption, StringComparison.Ordinal)
            ? null
            : NormalizeQuery(value);

    private IReadOnlyList<IArticleRecord> SortArticles(IReadOnlyList<IArticleRecord> results)
        => SelectedSortOption switch
        {
            SortByCreatedAscOption => results.OrderBy(article => article.CreatedAt).ToArray(),
            SortByLengthDescOption => results.OrderByDescending(article => article.RawText.Length).ToArray(),
            SortByLengthAscOption => results.OrderBy(article => article.RawText.Length).ToArray(),
            _ => results.OrderByDescending(article => article.CreatedAt).ToArray(),
        };

    private static string BuildPreview(string rawText)
    {
        string normalized = rawText
            .Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace('\n', ' ')
            .Replace('\r', ' ')
            .Trim();

        if (normalized.Length <= 56)
        {
            return normalized;
        }

        return normalized[..56] + "…";
    }
}
