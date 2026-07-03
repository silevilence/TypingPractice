namespace TypingCore.Abstractions;

/// <summary>
/// Represents a platform-neutral key input event that can be consumed by the typing engine.
/// </summary>
/// <remarks>
/// Implementations should behave like immutable value objects so callers may safely pass them across threads.
/// </remarks>
public interface IKeyInputEvent
{
    /// <summary>
    /// Gets the normalized key classification for this event.
    /// </summary>
    KeyInputKey Key { get; }

    /// <summary>
    /// Gets the time when the input event was observed by the front end.
    /// </summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets a value indicating whether the event originated from an IME commit or composition path.
    /// </summary>
    bool IsFromIme { get; }

    /// <summary>
    /// Gets the committed text produced by an IME, if any.
    /// </summary>
    string? ImeCommitText { get; }

    /// <summary>
    /// Gets a value indicating whether the event represents a backspace action.
    /// </summary>
    bool IsBackspace { get; }
}