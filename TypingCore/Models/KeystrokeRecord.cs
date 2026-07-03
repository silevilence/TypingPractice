using TypingCore.Abstractions;

namespace TypingCore.Models;

/// <summary>
/// Represents an immutable keystroke record captured during a typing session.
/// </summary>
/// <remarks>
/// Instances are immutable value snapshots and are safe for concurrent reads.
/// </remarks>
public sealed record KeystrokeRecord
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KeystrokeRecord"/> class.
    /// </summary>
    /// <param name="timestamp">The time when the keystroke was observed.</param>
    /// <param name="key">The normalized key classification.</param>
    /// <param name="isFromIme">A value indicating whether the record originated from an IME path.</param>
    /// <param name="imeCommitText">The committed IME text, if any.</param>
    /// <param name="isBackspace">A value indicating whether the keystroke represents a backspace action.</param>
    public KeystrokeRecord(
        DateTimeOffset timestamp,
        KeyInputKey key,
        bool isFromIme,
        string? imeCommitText,
        bool isBackspace)
    {
        Timestamp = timestamp;
        Key = key;
        IsFromIme = isFromIme;
        ImeCommitText = imeCommitText;
        IsBackspace = isBackspace;
    }

    /// <summary>
    /// Gets the time when the keystroke was observed.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the normalized key classification.
    /// </summary>
    public KeyInputKey Key { get; }

    /// <summary>
    /// Gets a value indicating whether the record originated from an IME path.
    /// </summary>
    public bool IsFromIme { get; }

    /// <summary>
    /// Gets the committed IME text, if any.
    /// </summary>
    public string? ImeCommitText { get; }

    /// <summary>
    /// Gets a value indicating whether the keystroke represents a backspace action.
    /// </summary>
    public bool IsBackspace { get; }

    /// <summary>
    /// Gets a value indicating whether this record contains committed output text.
    /// </summary>
    public bool IsCommitted => !string.IsNullOrEmpty(ImeCommitText);
}