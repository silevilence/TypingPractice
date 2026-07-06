namespace TypingCore.Models;

/// <summary>
/// Stores platform-neutral user preferences for appearance and typing commands.
/// </summary>
/// <remarks>
/// Instances are immutable and safe for concurrent reads.
/// </remarks>
public sealed record UserPreferences
{
    /// <summary>
    /// Gets the default application preferences.
    /// </summary>
    public static UserPreferences Default { get; } = new(
        UserTheme.FollowSystem,
        "Microsoft YaHei UI",
        18d,
        "Ctrl+P",
        "Ctrl+R",
        "Ctrl+L");

    /// <summary>
    /// Initializes a new instance of the <see cref="UserPreferences"/> class.
    /// </summary>
    public UserPreferences(
        UserTheme theme,
        string fontFamily,
        double fontSize,
        string pauseShortcut,
        string restartShortcut,
        string toggleLayoutShortcut)
    {
        if (!Enum.IsDefined(theme))
        {
            throw new ArgumentOutOfRangeException(nameof(theme));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(fontFamily);
        ArgumentException.ThrowIfNullOrWhiteSpace(pauseShortcut);
        ArgumentException.ThrowIfNullOrWhiteSpace(restartShortcut);
        ArgumentException.ThrowIfNullOrWhiteSpace(toggleLayoutShortcut);

        if (fontSize is < 12d or > 32d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fontSize),
                "字号必须在 12 到 32 之间。");
        }

        Theme = theme;
        FontFamily = fontFamily;
        FontSize = fontSize;
        PauseShortcut = pauseShortcut;
        RestartShortcut = restartShortcut;
        ToggleLayoutShortcut = toggleLayoutShortcut;
    }

    /// <summary>
    /// Gets the selected visual theme.
    /// </summary>
    public UserTheme Theme { get; }

    /// <summary>
    /// Gets the preferred typing font family.
    /// </summary>
    public string FontFamily { get; }

    /// <summary>
    /// Gets the preferred typing font size.
    /// </summary>
    public double FontSize { get; }

    /// <summary>
    /// Gets the pause or resume shortcut.
    /// </summary>
    public string PauseShortcut { get; }

    /// <summary>
    /// Gets the restart shortcut.
    /// </summary>
    public string RestartShortcut { get; }

    /// <summary>
    /// Gets the layout-switch shortcut.
    /// </summary>
    public string ToggleLayoutShortcut { get; }
}
