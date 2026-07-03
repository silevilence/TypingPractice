namespace TypingCore.Abstractions;

/// <summary>
/// Represents a read-only statistics snapshot for a typing session.
/// </summary>
/// <remarks>
/// Implementations should be immutable snapshot objects and are expected to be safe for concurrent reads.
/// </remarks>
public interface IStatisticsSnapshot
{
    /// <summary>
    /// Gets the current keystrokes per minute value.
    /// </summary>
    double KeystrokesPerMinute { get; }

    /// <summary>
    /// Gets the current committed characters per minute value.
    /// </summary>
    double CharactersPerMinute { get; }

    /// <summary>
    /// Gets the current words per minute value.
    /// </summary>
    double WordsPerMinute { get; }

    /// <summary>
    /// Gets the current average code length.
    /// </summary>
    double AverageCodeLength { get; }

    /// <summary>
    /// Gets the number of backspace actions counted for the session.
    /// </summary>
    int BackspaceCount { get; }

    /// <summary>
    /// Gets the current error rate, expressed as a value between 0 and 1.
    /// </summary>
    double ErrorRate { get; }

    /// <summary>
    /// Gets the elapsed duration for the session.
    /// </summary>
    TimeSpan Elapsed { get; }
}