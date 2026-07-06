namespace TypingCore.Wpf.Services;

/// <summary>
/// Defines article file selection behavior for the WPF shell.
/// </summary>
public interface IFileDialogService
{
    /// <summary>
    /// Prompts the user to select an article file.
    /// </summary>
    /// <returns>The selected file path, or <see langword="null"/> when the dialog is cancelled.</returns>
    string? SelectArticleFile();

    /// <summary>
    /// Prompts the user to select a text code-table file.
    /// </summary>
    /// <returns>The selected file path, or <see langword="null"/> when the dialog is cancelled.</returns>
    string? SelectCodeTableFile();
}
