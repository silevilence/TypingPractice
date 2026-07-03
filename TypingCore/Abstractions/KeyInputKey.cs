namespace TypingCore.Abstractions;

/// <summary>
/// Identifies a platform-neutral key category that can be translated from WPF or other front ends.
/// </summary>
/// <remarks>
/// Enum values are immutable and thread-safe.
/// </remarks>
public enum KeyInputKey
{
    Unknown = 0,
    Character = 1,
    Backspace = 2,
    Enter = 3,
    Escape = 4,
    Space = 5,
    Tab = 6,
    Delete = 7,
    LeftArrow = 8,
    RightArrow = 9,
    UpArrow = 10,
    DownArrow = 11,
}