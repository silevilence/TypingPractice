using TypingCore.Abstractions;
using TypingCore.Models;

namespace TypingCore.Wpf.Services;

internal sealed class WindowMessageInputTranslator
{
    internal const int WmKeyDown = 0x0100;
    internal const int WmChar = 0x0102;
    internal const int WmImeChar = 0x0286;

    private static readonly TimeSpan DuplicateTextInputWindow = TimeSpan.FromMilliseconds(150);

    private readonly ISystemClock systemClock;
    private string? recentMessageCommitText;
    private DateTimeOffset recentMessageCommitTimestamp;

    public WindowMessageInputTranslator(ISystemClock systemClock)
    {
        this.systemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
    }

    public IKeyInputEvent? TranslateWindowMessage(int message, nint wParam)
    {
        DateTimeOffset timestamp = systemClock.UtcNow;

        if (message == WmKeyDown)
        {
            KeyInputKey key = TranslateVirtualKey(unchecked((int)wParam));
            return key == KeyInputKey.Unknown
                ? null
                : new KeyInputEvent(key, timestamp, false, null, key == KeyInputKey.Backspace);
        }

        if (message is not (WmChar or WmImeChar))
        {
            return null;
        }

        string? commitText = DecodeCommitText(wParam);
        if (string.IsNullOrEmpty(commitText))
        {
            return null;
        }

        string normalizedCommitText = NormalizeCommitText(commitText);
        recentMessageCommitText = normalizedCommitText;
        recentMessageCommitTimestamp = timestamp;

        return new KeyInputEvent(
            ClassifyCommitKey(normalizedCommitText),
            timestamp,
            message == WmImeChar,
            normalizedCommitText,
            false);
    }

    public IKeyInputEvent? TranslateTextInput(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        DateTimeOffset timestamp = systemClock.UtcNow;
        string normalizedText = NormalizeCommitText(text);

        if (ShouldSuppressDuplicateTextInput(normalizedText, timestamp))
        {
            recentMessageCommitText = null;
            recentMessageCommitTimestamp = default;
            return null;
        }

        return new KeyInputEvent(
            ClassifyCommitKey(normalizedText),
            timestamp,
            false,
            normalizedText,
            false);
    }

    public void Reset()
    {
        recentMessageCommitText = null;
        recentMessageCommitTimestamp = default;
    }

    private bool ShouldSuppressDuplicateTextInput(string text, DateTimeOffset timestamp)
    {
        return recentMessageCommitText is not null
            && string.Equals(recentMessageCommitText, text, StringComparison.Ordinal)
            && timestamp - recentMessageCommitTimestamp <= DuplicateTextInputWindow;
    }

    private static string NormalizeCommitText(string text)
        => text.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal);

    private static string? DecodeCommitText(nint wParam)
    {
        int codeUnit = unchecked((int)wParam) & 0xFFFF;

        if (codeUnit == 0)
        {
            return null;
        }

        if (codeUnit < 0x20 && codeUnit is not ('\r' or '\n' or '\t'))
        {
            return null;
        }

        return ((char)codeUnit).ToString();
    }

    private static KeyInputKey ClassifyCommitKey(string text)
    {
        if (string.Equals(text, " ", StringComparison.Ordinal))
        {
            return KeyInputKey.Space;
        }

        if (string.Equals(text, "\t", StringComparison.Ordinal))
        {
            return KeyInputKey.Tab;
        }

        if (string.Equals(text, "\n", StringComparison.Ordinal))
        {
            return KeyInputKey.Enter;
        }

        return KeyInputKey.Character;
    }

    private static KeyInputKey TranslateVirtualKey(int virtualKey)
    {
        return virtualKey switch
        {
            0x08 => KeyInputKey.Backspace,
            0x09 => KeyInputKey.Tab,
            0x0D => KeyInputKey.Enter,
            0x1B => KeyInputKey.Escape,
            0x20 => KeyInputKey.Space,
            0x25 => KeyInputKey.LeftArrow,
            0x26 => KeyInputKey.UpArrow,
            0x27 => KeyInputKey.RightArrow,
            0x28 => KeyInputKey.DownArrow,
            0x2E => KeyInputKey.Delete,
            >= 0x30 and <= 0x5A => KeyInputKey.Character,
            >= 0x60 and <= 0x6F => KeyInputKey.Character,
            >= 0xBA and <= 0xC0 => KeyInputKey.Character,
            >= 0xDB and <= 0xDF => KeyInputKey.Character,
            0xE2 => KeyInputKey.Character,
            _ => KeyInputKey.Unknown,
        };
    }
}