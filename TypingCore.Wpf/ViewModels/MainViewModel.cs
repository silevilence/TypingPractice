using TypingCore.Abstractions;
using TypingCore.Parsing;
using TypingCore.Wpf.Services;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TypingCore.Wpf.ViewModels;

/// <summary>
/// Coordinates the stage-seven shell navigation.
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    private readonly ArticleLibraryViewModel articleLibrary;
    private readonly IArticleTextLayoutBuilder articleTextLayoutBuilder;
    private readonly HistoryViewModel history;
    private readonly ISessionRepository sessionRepository;
    private readonly SettingsViewModel settings;
    private readonly ISystemClock systemClock;
    private PageViewModel currentPage;

    public MainViewModel(
        ArticleLibraryViewModel articleLibrary,
        HistoryViewModel history,
        SettingsViewModel settings,
        IArticleTextLayoutBuilder articleTextLayoutBuilder,
        ISystemClock systemClock,
        ISessionRepository sessionRepository)
    {
        ArgumentNullException.ThrowIfNull(articleLibrary);
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(articleTextLayoutBuilder);
        ArgumentNullException.ThrowIfNull(systemClock);
        ArgumentNullException.ThrowIfNull(sessionRepository);

        this.articleLibrary = articleLibrary;
        this.articleTextLayoutBuilder = articleTextLayoutBuilder;
        this.history = history;
        this.sessionRepository = sessionRepository;
        this.settings = settings;
        this.systemClock = systemClock;
        currentPage = articleLibrary;

        ShowArticleLibraryCommand = new RelayCommand(ShowArticleLibrary);
        ShowHistoryCommand = new AsyncRelayCommand(() => ShowHistoryAsync());
        ShowSettingsCommand = new RelayCommand(ShowSettings);
        articleLibrary.PracticeRequested += HandlePracticeRequested;
    }

    public PageViewModel CurrentPage
    {
        get => currentPage;
        private set
        {
            if (SetProperty(ref currentPage, value))
            {
                OnPropertyChanged(nameof(IsArticleLibrarySelected));
                OnPropertyChanged(nameof(IsHistorySelected));
                OnPropertyChanged(nameof(IsSettingsSelected));
            }
        }
    }

    public bool IsArticleLibrarySelected => ReferenceEquals(CurrentPage, articleLibrary);

    public bool IsHistorySelected => ReferenceEquals(CurrentPage, history);

    public bool IsSettingsSelected => ReferenceEquals(CurrentPage, settings);

    public IRelayCommand ShowArticleLibraryCommand { get; }

    public IAsyncRelayCommand ShowHistoryCommand { get; }

    public IRelayCommand ShowSettingsCommand { get; }

    public Task InitializeAsync() => articleLibrary.LoadAsync();

    private void ShowArticleLibrary() => CurrentPage = articleLibrary;

    private void ShowSettings() => CurrentPage = settings;

    private void HandlePracticeRequested(IArticleRecord article)
        => StartPractice(article);

    private void StartPractice(IArticleRecord article)
    {
        TypingPracticeViewModel? practice = null;
        practice = new TypingPracticeViewModel(
            article,
            articleTextLayoutBuilder,
            systemClock,
            ShowArticleLibrary,
            sessionRepository,
            (completedArticle, statistics, errorCharacterCount) =>
            {
                if (ReferenceEquals(CurrentPage, practice))
                {
                    ShowResult(completedArticle, statistics, errorCharacterCount);
                }
            });
        CurrentPage = practice;
    }

    private void ShowResult(
        IArticleRecord article,
        IStatisticsSnapshot statistics,
        int errorCharacterCount)
    {
        CurrentPage = new PracticeResultViewModel(
            article,
            statistics,
            errorCharacterCount,
            () => StartPractice(article),
            () => ShowHistoryAsync(article.ArticleId),
            ShowArticleLibrary);
    }

    private async Task ShowHistoryAsync(string? preferredArticleId = null)
    {
        CurrentPage = history;
        await history.LoadAsync(preferredArticleId);
    }
}
