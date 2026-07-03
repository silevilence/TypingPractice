namespace TypingCore.Wpf.Services;

/// <summary>
/// Defines clipboard read behavior for article imports.
/// </summary>
public interface IClipboardService
{
    /// <summary>
    /// Reads plain text from the clipboard.
    /// </summary>
    /// <returns>The clipboard text, or <see langword="null"/> when no text is available.</returns>
    string? ReadText();
}