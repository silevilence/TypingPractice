namespace TypingCore.Abstractions;

/// <summary>
/// Defines article lookup operations for the typing application.
/// </summary>
/// <remarks>
/// Implementations own persistence concerns. Thread safety depends on the chosen adapter.
/// </remarks>
public interface IArticleRepository
{
    /// <summary>
    /// Gets a single article by its identifier.
    /// </summary>
    /// <param name="articleId">The unique article identifier.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>The matching article, or <see langword="null"/> when not found.</returns>
    Task<IArticleRecord?> GetByIdAsync(string articleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches articles by free-text query and optional tag.
    /// </summary>
    /// <param name="query">The title or content query, if any.</param>
    /// <param name="tag">The tag filter, if any.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>A read-only list of article records that match the query.</returns>
    Task<IReadOnlyList<IArticleRecord>> SearchAsync(
        string? query = null,
        string? tag = null,
        CancellationToken cancellationToken = default);
}