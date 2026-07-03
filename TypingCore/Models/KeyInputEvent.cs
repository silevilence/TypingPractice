using TypingCore.Abstractions;

namespace TypingCore.Models;

/// <summary>
/// Represents an immutable normalized input event for the typing engine.
/// </summary>
/// <remarks>
/// Instances are immutable value snapshots and are safe for concurrent reads.
/// </remarks>
public sealed record KeyInputEvent : IKeyInputEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KeyInputEvent"/> class.
    /// </summary>
    /// <param name="key">The normalized key classification.</param>
    /// <param name="timestamp">The time when the input event was observed.</param>
    /// <param name="isFromIme">A value indicating whether the event originated from an IME path.</param>
    /// <param name="imeCommitText">The committed text payload, if any.</param>
    /// <param name="isBackspace">A value indicating whether the event represents a backspace action.</param>
    public KeyInputEvent(
        KeyInputKey key,
        DateTimeOffset timestamp,
        bool isFromIme,
        string? imeCommitText,
        bool isBackspace)
    {
        Key = key;
        Timestamp = timestamp;
        IsFromIme = isFromIme;
        ImeCommitText = imeCommitText;
        IsBackspace = isBackspace;
    }

    /// <inheritdoc />
    public KeyInputKey Key { get; }

    /// <inheritdoc />
    public DateTimeOffset Timestamp { get; }

    /// <inheritdoc />
    public bool IsFromIme { get; }

    /// <inheritdoc />
    public string? ImeCommitText { get; }

    /// <inheritdoc />
    public bool IsBackspace { get; }
}