namespace TypingCore.Abstractions;

/// <summary>
/// Represents the comparison result for a single target character.
/// </summary>
/// <remarks>
/// Instances are immutable value snapshots and are safe for concurrent reads.
/// </remarks>
public sealed record TypingCharacterSnapshot
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypingCharacterSnapshot"/> class.
    /// </summary>
    /// <param name="textIndex">The zero-based index in the target text.</param>
    /// <param name="targetChar">The target character.</param>
    /// <param name="inputChar">The committed character entered for this position, if any.</param>
    /// <param name="state">The current comparison state.</param>
    public TypingCharacterSnapshot(int textIndex, char targetChar, char? inputChar, TypingCharacterState state)
    {
        TextIndex = textIndex;
        TargetChar = targetChar;
        InputChar = inputChar;
        State = state;
    }

    /// <summary>
    /// Gets the zero-based index in the target text.
    /// </summary>
    public int TextIndex { get; }

    /// <summary>
    /// Gets the target character.
    /// </summary>
    public char TargetChar { get; }

    /// <summary>
    /// Gets the committed character entered for this position, if any.
    /// </summary>
    public char? InputChar { get; }

    /// <summary>
    /// Gets the current comparison state.
    /// </summary>
    public TypingCharacterState State { get; }
}