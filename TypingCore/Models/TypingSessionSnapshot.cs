using TypingCore.Abstractions;

namespace TypingCore.Models;

/// <summary>
/// Represents an immutable snapshot of the current typing session state.
/// </summary>
/// <remarks>
/// Instances copy character inputs on construction and are safe for concurrent reads.
/// </remarks>
public sealed record TypingSessionSnapshot : ITypingSessionSnapshot
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypingSessionSnapshot"/> class.
    /// </summary>
    /// <param name="state">The lifecycle state.</param>
    /// <param name="currentTextIndex">The zero-based index of the current target character.</param>
    /// <param name="correctCharacterCount">The number of correctly committed characters.</param>
    /// <param name="errorCharacterCount">The number of committed characters currently marked incorrect.</param>
    /// <param name="characters">The per-character comparison results.</param>
    public TypingSessionSnapshot(
        TypingSessionState state,
        int currentTextIndex,
        int correctCharacterCount,
        int errorCharacterCount,
        IEnumerable<TypingCharacterSnapshot> characters)
        : this(
            state,
            currentTextIndex,
            correctCharacterCount,
            errorCharacterCount,
            characters?.ToArray() ?? throw new ArgumentNullException(nameof(characters)))
    {
    }

    internal TypingSessionSnapshot(
        TypingSessionState state,
        int currentTextIndex,
        int correctCharacterCount,
        int errorCharacterCount,
        TypingCharacterSnapshot[] characters)
    {
        State = state;
        CurrentTextIndex = currentTextIndex;
        CorrectCharacterCount = correctCharacterCount;
        ErrorCharacterCount = errorCharacterCount;
        Characters = characters;
    }

    /// <inheritdoc />
    public TypingSessionState State { get; }

    /// <inheritdoc />
    public int CurrentTextIndex { get; }

    /// <inheritdoc />
    public int CorrectCharacterCount { get; }

    /// <inheritdoc />
    public int ErrorCharacterCount { get; }

    /// <inheritdoc />
    public IReadOnlyList<TypingCharacterSnapshot> Characters { get; }
}
