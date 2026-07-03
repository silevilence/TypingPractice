namespace TypingCore.Abstractions;

/// <summary>
/// Describes the lifecycle state of a typing session.
/// </summary>
/// <remarks>
/// Enum values are immutable and thread-safe.
/// </remarks>
public enum TypingSessionState
{
    NotStarted = 0,
    Running = 1,
    Paused = 2,
    Completed = 3,
    Cancelled = 4,
}