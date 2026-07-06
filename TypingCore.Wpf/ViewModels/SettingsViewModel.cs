using CommunityToolkit.Mvvm.Input;
using TypingCore.Abstractions;
using TypingCore.Models;

namespace TypingCore.Wpf.ViewModels;

/// <summary>
/// Edits and persists application appearance and shortcut preferences.
/// </summary>
public sealed class SettingsViewModel : PageViewModel
{
    private readonly Action<UserPreferences>? preferencesApplied;
    private readonly IUserPreferencesRepository? repository;
    private UserPreferences activePreferences = UserPreferences.Default;
    private string pauseShortcut = UserPreferences.Default.PauseShortcut;
    private string restartShortcut = UserPreferences.Default.RestartShortcut;
    private string selectedFont = UserPreferences.Default.FontFamily;
    private double selectedFontSize = UserPreferences.Default.FontSize;
    private string selectedTheme = "跟随系统";
    private string statusMessage = "更改主题和字体会立即预览，点击保存后写入本地配置。";
    private string toggleLayoutShortcut = UserPreferences.Default.ToggleLayoutShortcut;

    public SettingsViewModel(
        IUserPreferencesRepository? repository = null,
        Action<UserPreferences>? preferencesApplied = null)
        : base("设置")
    {
        this.repository = repository;
        this.preferencesApplied = preferencesApplied;

        ThemeOptions = ["跟随系统", "晨纸浅色", "夜色深色"];
        FontOptions = ["Microsoft YaHei UI", "Segoe UI", "Consolas"];
        FontSizeOptions = [16d, 18d, 20d, 22d, 24d, 28d, 32d];
        ShortcutOptions =
        [
            "Ctrl+P",
            "Ctrl+R",
            "Ctrl+L",
            "Ctrl+Space",
            "Ctrl+Tab",
            "F5",
            "F6",
            "F7",
        ];
        SaveCommand = new AsyncRelayCommand(SaveAsync);
    }

    public IReadOnlyList<string> ThemeOptions { get; }

    public IReadOnlyList<string> FontOptions { get; }

    public IReadOnlyList<double> FontSizeOptions { get; }

    public IReadOnlyList<string> ShortcutOptions { get; }

    public IAsyncRelayCommand SaveCommand { get; }

    public UserPreferences CurrentPreferences => activePreferences;

    public string SelectedTheme
    {
        get => selectedTheme;
        set
        {
            if (SetProperty(ref selectedTheme, value))
            {
                ApplyAppearance();
            }
        }
    }

    public string SelectedFont
    {
        get => selectedFont;
        set
        {
            if (SetProperty(ref selectedFont, value))
            {
                ApplyAppearance();
            }
        }
    }

    public double SelectedFontSize
    {
        get => selectedFontSize;
        set
        {
            if (SetProperty(ref selectedFontSize, value))
            {
                ApplyAppearance();
            }
        }
    }

    public string PauseShortcut
    {
        get => pauseShortcut;
        set => SetProperty(ref pauseShortcut, value);
    }

    public string RestartShortcut
    {
        get => restartShortcut;
        set => SetProperty(ref restartShortcut, value);
    }

    public string ToggleLayoutShortcut
    {
        get => toggleLayoutShortcut;
        set => SetProperty(ref toggleLayoutShortcut, value);
    }

    public string StatusMessage
    {
        get => statusMessage;
        private set => SetProperty(ref statusMessage, value);
    }

    public async Task LoadAsync()
    {
        if (repository is null)
        {
            preferencesApplied?.Invoke(activePreferences);
            return;
        }

        try
        {
            Apply(await repository.LoadAsync());
            StatusMessage = "已加载本地偏好设置。";
        }
        catch (Exception ex)
        {
            Apply(UserPreferences.Default);
            StatusMessage = $"偏好设置读取失败，已使用默认值：{ex.Message}";
        }
    }

    public async Task SaveAsync()
    {
        UserPreferences preferences = CreatePreferences();
        string[] shortcuts =
        [
            preferences.PauseShortcut,
            preferences.RestartShortcut,
            preferences.ToggleLayoutShortcut,
        ];
        if (shortcuts.Distinct(StringComparer.OrdinalIgnoreCase).Count() != shortcuts.Length)
        {
            StatusMessage = "暂停、重来和切换布局不能使用相同快捷键。";
            return;
        }

        try
        {
            if (repository is not null)
            {
                await repository.SaveAsync(preferences);
            }

            activePreferences = preferences;
            preferencesApplied?.Invoke(preferences);
            StatusMessage = "设置已保存并应用。";
        }
        catch (Exception ex)
        {
            StatusMessage = $"设置保存失败：{ex.Message}";
        }
    }

    private void Apply(UserPreferences preferences)
    {
        activePreferences = preferences;
        selectedTheme = GetThemeName(preferences.Theme);
        selectedFont = preferences.FontFamily;
        selectedFontSize = preferences.FontSize;
        pauseShortcut = preferences.PauseShortcut;
        restartShortcut = preferences.RestartShortcut;
        toggleLayoutShortcut = preferences.ToggleLayoutShortcut;

        OnPropertyChanged(nameof(SelectedTheme));
        OnPropertyChanged(nameof(SelectedFont));
        OnPropertyChanged(nameof(SelectedFontSize));
        OnPropertyChanged(nameof(PauseShortcut));
        OnPropertyChanged(nameof(RestartShortcut));
        OnPropertyChanged(nameof(ToggleLayoutShortcut));
        preferencesApplied?.Invoke(preferences);
    }

    private void ApplyAppearance()
    {
        try
        {
            preferencesApplied?.Invoke(CreatePreferences());
        }
        catch (Exception ex)
        {
            StatusMessage = $"设置无法应用：{ex.Message}";
        }
    }

    private UserPreferences CreatePreferences()
        => new(
            GetTheme(SelectedTheme),
            SelectedFont,
            SelectedFontSize,
            PauseShortcut,
            RestartShortcut,
            ToggleLayoutShortcut);

    private static UserTheme GetTheme(string value)
        => value switch
        {
            "晨纸浅色" => UserTheme.Light,
            "夜色深色" => UserTheme.Dark,
            _ => UserTheme.FollowSystem,
        };

    private static string GetThemeName(UserTheme value)
        => value switch
        {
            UserTheme.Light => "晨纸浅色",
            UserTheme.Dark => "夜色深色",
            _ => "跟随系统",
        };
}
