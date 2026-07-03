using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TypingCore.Wpf.ViewModels;

/// <summary>
/// Coordinates the stage-seven shell navigation.
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    private readonly ArticleLibraryViewModel articleLibrary;
    private readonly SettingsViewModel settings;
    private PageViewModel currentPage;

    public MainViewModel(ArticleLibraryViewModel articleLibrary, SettingsViewModel settings)
    {
        ArgumentNullException.ThrowIfNull(articleLibrary);
        ArgumentNullException.ThrowIfNull(settings);

        this.articleLibrary = articleLibrary;
        this.settings = settings;
        currentPage = articleLibrary;

        ShowArticleLibraryCommand = new RelayCommand(ShowArticleLibrary);
        ShowSettingsCommand = new RelayCommand(ShowSettings);
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
}