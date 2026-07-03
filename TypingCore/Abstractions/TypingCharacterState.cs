namespace TypingCore.Abstractions;

/// <summary>
/// Describes the comparison state of a target character in the current typing session.
/// </summary>
/// <remarks>
/// Enum values are immutable and thread-safe.
/// </remarks>
public enum TypingCharacterState
{
    Pending = 0,
    Current = 1,
    Correct = 2,
    Incorrect = 3,
}