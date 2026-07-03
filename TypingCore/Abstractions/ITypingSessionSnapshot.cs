namespace TypingCore.Abstractions;

/// <summary>
/// Represents a read-only snapshot of a typing session at a point in time.
/// </summary>
/// <remarks>
/// Implementations should be immutable snapshots so they can be safely cached or read from multiple threads.
/// </remarks>
public interface ITypingSessionSnapshot
{
    /// <summary>
    /// Gets the lifecycle state of the session.
    /// </summary>
    TypingSessionState State { get; }

    /// <summary>
    /// Gets the zero-based index of the current target character.
    /// </summary>
    int CurrentTextIndex { get; }

    /// <summary>
    /// Gets the number of correctly committed characters.
    /// </summary>
    int CorrectCharacterCount { get; }

    /// <summary>
    /// Gets the number of committed characters currently marked as incorrect.
    /// </summary>
    int ErrorCharacterCount { get; }
}