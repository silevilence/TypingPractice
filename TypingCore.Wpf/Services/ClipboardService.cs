using System.Windows;

namespace TypingCore.Wpf.Services;

internal sealed class ClipboardService : IClipboardService
{
    public string? ReadText()
    {
        if (!Clipboard.ContainsText())
        {
            return null;
        }

        string text = Clipboard.GetText();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }
}