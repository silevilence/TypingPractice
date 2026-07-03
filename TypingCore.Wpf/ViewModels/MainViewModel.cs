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
    private readonly SettingsViewModel settings;
    private readonly ISystemClock systemClock;
    private PageViewModel currentPage;

    public MainViewModel(
        ArticleLibraryViewModel articleLibrary,
        SettingsViewModel settings,
        IArticleTextLayoutBuilder articleTextLayoutBuilder,
        ISystemClock systemClock)
    {
        ArgumentNullException.ThrowIfNull(articleLibrary);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(articleTextLayoutBuilder);
        ArgumentNullException.ThrowIfNull(systemClock);

        this.articleLibrary = articleLibrary;
        this.articleTextLayoutBuilder = articleTextLayoutBuilder;
        this.settings = settings;
        this.systemClock = systemClock;
        currentPage = articleLibrary;

        ShowArticleLibraryCommand = new RelayCommand(ShowArticleLibrary);
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
                OnPropertyChanged(nameof(IsSettingsSelected));
            }
        }
    }

    public bool IsArticleLibrarySelected => ReferenceEquals(CurrentPage, articleLibrary);

    public bool IsSettingsSelected => ReferenceEquals(CurrentPage, settings);

    public IRelayCommand ShowArticleLibraryCommand { get; }

    public IRelayCommand ShowSettingsCommand { get; }

    public Task InitializeAsync() => articleLibrary.LoadAsync();

    private void ShowArticleLibrary() => CurrentPage = articleLibrary;

    private void ShowSettings() => CurrentPage = settings;

    private void HandlePracticeRequested(IArticleRecord article)
    {
        CurrentPage = new TypingPracticeViewModel(
            article,
            articleTextLayoutBuilder,
            systemClock,
            ShowArticleLibrary);
    }
}