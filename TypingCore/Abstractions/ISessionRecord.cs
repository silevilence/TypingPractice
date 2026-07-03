namespace TypingCore.Abstractions;

/// <summary>
/// Represents session data exchanged through repository boundaries.
/// </summary>
/// <remarks>
/// Implementations should be immutable snapshots. Concurrent read safety follows from immutability.
/// </remarks>
public interface ISessionRecord
{
    /// <summary>
    /// Gets the unique session identifier.
    /// </summary>
    string SessionId { get; }

    /// <summary>
    /// Gets the related article identifier.
    /// </summary>
    string ArticleId { get; }

    /// <summary>
    /// Gets the session start timestamp.
    /// </summary>
    DateTimeOffset StartedAt { get; }

    /// <summary>
    /// Gets the session end timestamp, if the session has finished.
    /// </summary>
    DateTimeOffset? EndedAt { get; }

    /// <summary>
    /// Gets the persisted statistics snapshot, if one has been recorded.
    /// </summary>
    IStatisticsSnapshot? Statistics { get; }
}