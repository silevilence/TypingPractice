namespace TypingCore.Abstractions;

/// <summary>
/// Coordinates a single typing run by accepting normalized input events and exposing the current session snapshot.
/// </summary>
/// <remarks>
/// Implementations are not assumed to be thread-safe unless an adapter documents stronger guarantees.
/// </remarks>
public interface ITypingSession
{
    /// <summary>
    /// Gets the current session snapshot.
    /// </summary>
    ITypingSessionSnapshot Snapshot { get; }

    /// <summary>
    /// Gets the statistics provider associated with this session.
    /// </summary>
    IStatisticsProvider StatisticsProvider { get; }

    /// <summary>
    /// Feeds a normalized input event into the session.
    /// </summary>
    /// <param name="inputEvent">The event to process.</param>
    void ProcessInput(IKeyInputEvent inputEvent);

    /// <summary>
    /// Pauses a running session at the supplied timestamp.
    /// </summary>
    /// <param name="timestamp">The timestamp when the pause started.</param>
    void Pause(DateTimeOffset timestamp);

    /// <summary>
    /// Resumes a paused session at the supplied timestamp.
    /// </summary>
    /// <param name="timestamp">The timestamp when input resumes.</param>
    void Resume(DateTimeOffset timestamp);

    /// <summary>
    /// Resets the session back to its initial state.
    /// </summary>
    void Reset();
}
