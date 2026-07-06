using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TypingCore.Abstractions;
using TypingCore.Models;
using TypingCore.Parsing;
using TypingCore.Wpf.Services;
using TypingCore.Wpf.ViewModels;
using TypingCore.Wpf.Views;

namespace TypingCore.Tests.Wpf;

public sealed class PhaseThirteenSettingsTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 6, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task LoadAsync_restores_preferences_and_applies_appearance()
    {
        UserPreferences stored = new(
            UserTheme.Dark,
            "Consolas",
            24d,
            "F5",
            "F6",
            "F7");
        FakePreferencesRepository repository = new(stored);
        UserPreferences? applied = null;
        SettingsViewModel viewModel = new(repository, value => applied = value);

        await viewModel.LoadAsync();

        Assert.Equal("夜色深色", viewModel.SelectedTheme);
        Assert.Equal("Consolas", viewModel.SelectedFont);
        Assert.Equal(24d, viewModel.SelectedFontSize);
        Assert.Equal(stored, applied);
    }

    [Fact]
    public async Task SaveAsync_rejects_duplicate_shortcuts()
    {
        FakePreferencesRepository repository = new(UserPreferences.Default);
        SettingsViewModel viewModel = new(repository)
        {
            PauseShortcut = "F5",
            RestartShortcut = "F5",
            ToggleLayoutShortcut = "F7",
        };

        await viewModel.SaveAsync();

        Assert.Null(repository.SavedPreferences);
        Assert.Contains("不能使用相同快捷键", viewModel.StatusMessage);
    }

    [Fact]
    public void ShortcutOptions_use_valid_wpf_key_gestures()
    {
        KeyGestureConverter converter = new();

        foreach (string shortcut in new SettingsViewModel().ShortcutOptions)
        {
            Assert.IsType<KeyGesture>(converter.ConvertFromInvariantString(shortcut));
        }
    }

    [Fact]
    public void PauseCommand_pauses_ignores_input_and_resumes()
    {
        MutableSystemClock clock = new(Now);
        UserPreferences preferences = new(
            UserTheme.Light,
            "Consolas",
            22d,
            "F5",
            "F6",
            "F7");
        TypingPracticeViewModel viewModel = new(
            new Article("article-1", "练习", "ab", Now, []),
            new ArticleTextLayoutBuilder(),
            clock,
            () => { },
            preferences: preferences);

        viewModel.HandleTextInput("a");
        viewModel.PauseCommand.Execute(null);
        bool ignored = viewModel.HandleTextInput("b");

        Assert.True(viewModel.IsPaused);
        Assert.True(ignored);
        Assert.Equal(1, viewModel.CurrentTextIndex);
        Assert.Equal("F5", viewModel.PauseShortcut);
        Assert.Equal("Consolas", viewModel.PracticeFontFamily);
        Assert.Equal(22d, viewModel.PracticeFontSize);

        clock.Advance(TimeSpan.FromMinutes(1));
        viewModel.PauseCommand.Execute(null);
        viewModel.HandleTextInput("b");

        Assert.True(viewModel.IsCompleted);
        Assert.False(viewModel.IsPaused);
    }

    [Fact]
    public void SettingsView_renders_complete_controls()
    {
        Exception? failure = null;
        Thread thread = new(() =>
        {
            try
            {
                if (Application.Current is null)
                {
                    _ = new Application();
                }

                SettingsView view = new()
                {
                    DataContext = new SettingsViewModel(),
                };
                view.Measure(new Size(900d, 600d));
                view.Arrange(new Rect(0d, 0d, 900d, 600d));
                view.UpdateLayout();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    [Fact]
    public void ApplicationThemeManager_applies_dark_palette_and_font_resources()
    {
        ResourceDictionary resources = new();
        UserPreferences preferences = new(
            UserTheme.Dark,
            "Consolas",
            24d,
            "F5",
            "F6",
            "F7");

        ApplicationThemeManager.Apply(resources, preferences, useDarkTheme: true);

        SolidColorBrush background = Assert.IsType<SolidColorBrush>(
            resources["PaperBackgroundBrush"]);
        SolidColorBrush text = Assert.IsType<SolidColorBrush>(
            resources["PrimaryTextBrush"]);
        FontFamily font = Assert.IsType<FontFamily>(resources["PracticeFontFamily"]);

        Assert.Equal(Color.FromRgb(0x2D, 0x29, 0x26), background.Color);
        Assert.Equal(Color.FromRgb(0xFF, 0xFF, 0xFF), text.Color);
        Assert.Equal("Consolas", font.Source);
        Assert.Equal(24d, resources["PracticeFontSize"]);
    }

    private sealed class FakePreferencesRepository(UserPreferences stored)
        : IUserPreferencesRepository
    {
        public UserPreferences? SavedPreferences { get; private set; }

        public Task<UserPreferences> LoadAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(stored);

        public Task SaveAsync(
            UserPreferences preferences,
            CancellationToken cancellationToken = default)
        {
            SavedPreferences = preferences;
            return Task.CompletedTask;
        }
    }

    private sealed class MutableSystemClock(DateTimeOffset utcNow) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; private set; } = utcNow;

        public void Advance(TimeSpan duration) => UtcNow = UtcNow.Add(duration);
    }
}
