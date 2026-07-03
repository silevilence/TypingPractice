using TypingCore.Abstractions;

namespace TypingCore.Models;

/// <summary>
/// Represents an immutable statistics snapshot for a typing session.
/// </summary>
/// <remarks>
/// Instances are value snapshots and are safe for concurrent reads.
/// </remarks>
public sealed record SessionStatistics : IStatisticsSnapshot
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SessionStatistics"/> class.
    /// </summary>
    /// <param name="keystrokesPerMinute">The keystrokes per minute.</param>
    /// <param name="charactersPerMinute">The characters per minute.</param>
    /// <param name="wordsPerMinute">The words per minute.</param>
    /// <param name="averageCodeLength">The average code length.</param>
    /// <param name="backspaceCount">The backspace count.</param>
    /// <param name="backspaceRate">The backspace rate.</param>
    /// <param name="errorRate">The error rate.</param>
    /// <param name="elapsed">The elapsed duration.</param>
    public SessionStatistics(
        double keystrokesPerMinute,
        double charactersPerMinute,
        double wordsPerMinute,
        double averageCodeLength,
        int backspaceCount,
        double backspaceRate,
        double errorRate,
        TimeSpan elapsed)
    {
        KeystrokesPerMinute = keystrokesPerMinute;
        CharactersPerMinute = charactersPerMinute;
        WordsPerMinute = wordsPerMinute;
        AverageCodeLength = averageCodeLength;
        BackspaceCount = backspaceCount;
        BackspaceRate = backspaceRate;
        ErrorRate = errorRate;
        Elapsed = elapsed;
    }

    /// <inheritdoc />
    public double KeystrokesPerMinute { get; }

    /// <inheritdoc />
    public double CharactersPerMinute { get; }

    /// <inheritdoc />
    public double WordsPerMinute { get; }

    /// <inheritdoc />
    public double AverageCodeLength { get; }

    /// <inheritdoc />
    public int BackspaceCount { get; }

    /// <inheritdoc />
    public double BackspaceRate { get; }

    /// <inheritdoc />
    public double ErrorRate { get; }

    /// <inheritdoc />
    public TimeSpan Elapsed { get; }
}