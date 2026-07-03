using TypingCore.Abstractions;

namespace TypingCore.Models;

/// <summary>
/// Represents an immutable persisted typing session record.
/// </summary>
/// <remarks>
/// Instances carry only persisted identifiers and timestamps and are safe for concurrent reads.
/// </remarks>
public sealed record TypingSessionRecord : ISessionRecord
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypingSessionRecord"/> class.
    /// </summary>
    /// <param name="sessionId">The unique session identifier.</param>
    /// <param name="articleId">The related article identifier.</param>
    /// <param name="startedAt">The session start timestamp.</param>
    /// <param name="endedAt">The session end timestamp.</param>
    /// <param name="statistics">The persisted statistics snapshot, if any.</param>
    public TypingSessionRecord(
        string sessionId,
        string articleId,
        DateTimeOffset startedAt,
        DateTimeOffset? endedAt,
        IStatisticsSnapshot? statistics = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(articleId);

        SessionId = sessionId;
        ArticleId = articleId;
        StartedAt = startedAt;
        EndedAt = endedAt;
        Statistics = statistics;
    }

    /// <inheritdoc />
    public string SessionId { get; }

    /// <inheritdoc />
    public string ArticleId { get; }

    /// <inheritdoc />
    public DateTimeOffset StartedAt { get; }

    /// <inheritdoc />
    public DateTimeOffset? EndedAt { get; }

    /// <inheritdoc />
    public IStatisticsSnapshot? Statistics { get; }
}