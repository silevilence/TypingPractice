using Microsoft.Win32;
using System.Windows;
using System.Windows.Media;
using TypingCore.Models;

namespace TypingCore.Wpf.Services;

internal static class ApplicationThemeManager
{
    public static event EventHandler? ThemeChanged;

    public static void Apply(UserPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        bool useDarkTheme = preferences.Theme == UserTheme.Dark
            || preferences.Theme == UserTheme.FollowSystem && IsSystemDarkTheme();
        Apply(Application.Current.Resources, preferences, useDarkTheme);
    }

    internal static void Apply(
        ResourceDictionary resources,
        UserPreferences preferences,
        bool useDarkTheme)
    {
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(preferences);

        SetBrush(resources, "PaperBackgroundBrush", useDarkTheme ? "#2D2926" : "#FAF8F5");
        SetBrush(resources, "SidebarBackgroundBrush", useDarkTheme ? "#2D2926" : "#F3EFEB");
        SetBrush(resources, "CardBackgroundBrush", useDarkTheme ? "#2D2926" : "#FFFFFF");
        SetBrush(resources, "AccentBrush", useDarkTheme ? "#F0E6D6" : "#8B5E3C");
        SetBrush(resources, "AccentHoverBrush", useDarkTheme ? "#C4A882" : "#7A5234");
        SetBrush(resources, "TagBackgroundBrush", useDarkTheme ? "#8B5E3C" : "#F0E6D6");
        SetBrush(resources, "PrimaryTextBrush", useDarkTheme ? "#FFFFFF" : "#2D2926");
        SetBrush(resources, "SecondaryTextBrush", useDarkTheme ? "#F3EFEB" : "#7A7067");
        SetBrush(resources, "BorderBrush", useDarkTheme ? "#C4A882" : "#E8E2DA");
        SetBrush(resources, "ErrorBrush", useDarkTheme ? "#F0E6D6" : "#C75146");
        SetBrush(resources, "SuccessBrush", useDarkTheme ? "#C4A882" : "#5B8C5A");
        SetBrush(resources, "OnAccentTextBrush", useDarkTheme ? "#2D2926" : "#FFFFFF");
        resources["PracticeFontFamily"] = new FontFamily(preferences.FontFamily);
        resources["PracticeFontSize"] = preferences.FontSize;

        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }

    public static Brush FindBrush(FrameworkElement element, string resourceKey, Brush fallback)
        => element.TryFindResource(resourceKey) as Brush ?? fallback;

    private static bool IsSystemDarkTheme()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }
        catch
        {
            return false;
        }
    }

    private static void SetBrush(
        ResourceDictionary resources,
        string key,
        string colorValue)
    {
        Color color = (Color)ColorConverter.ConvertFromString(colorValue);
        SolidColorBrush brush = new(color);
        brush.Freeze();
        resources[key] = brush;
    }
}
