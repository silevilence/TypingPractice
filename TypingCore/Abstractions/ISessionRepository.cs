namespace TypingCore.Abstractions;

/// <summary>
/// Defines persistence operations for completed or in-progress typing sessions.
/// </summary>
/// <remarks>
/// Implementations own storage and indexing behavior. Thread safety depends on the adapter.
/// </remarks>
public interface ISessionRepository
{
    /// <summary>
    /// Saves a session record.
    /// </summary>
    /// <param name="sessionRecord">The session record to persist.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    Task SaveAsync(ISessionRecord sessionRecord, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets session records associated with a given article identifier.
    /// </summary>
    /// <param name="articleId">The related article identifier.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>A read-only list of related session records.</returns>
    Task<IReadOnlyList<ISessionRecord>> GetByArticleIdAsync(
        string articleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets session records that fall within the specified time range.
    /// </summary>
    /// <param name="startInclusive">The inclusive range start.</param>
    /// <param name="endExclusive">The exclusive range end.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>A read-only list of matching session records.</returns>
    Task<IReadOnlyList<ISessionRecord>> GetByTimeRangeAsync(
        DateTimeOffset startInclusive,
        DateTimeOffset endExclusive,
        CancellationToken cancellationToken = default);
}