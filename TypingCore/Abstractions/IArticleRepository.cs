using TypingCore.Models;

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
    /// Saves an article record.
    /// </summary>
    /// <param name="articleRecord">The article record to persist.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    Task SaveAsync(IArticleRecord articleRecord, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing article record.
    /// </summary>
    /// <param name="articleRecord">The article record to persist.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    Task UpdateAsync(IArticleRecord articleRecord, CancellationToken cancellationToken = default)
        => SaveAsync(articleRecord, cancellationToken);

    /// <summary>
    /// Soft-deletes an article by its identifier.
    /// </summary>
    /// <param name="articleId">The unique article identifier.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    Task DeleteAsync(string articleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a soft-deleted article by its identifier.
    /// </summary>
    /// <param name="articleId">The unique article identifier.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    Task RestoreAsync(string articleId, CancellationToken cancellationToken = default);

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

    /// <summary>
    /// Gets all tags currently attached to non-deleted articles.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>A sorted read-only list of distinct tags.</returns>
    async Task<IReadOnlyList<string>> GetTagsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<IArticleRecord> articles = await SearchAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return articles
            .SelectMany(article => article.Tags)
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(tag => tag, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Adds a tag to an article when it is not already present.
    /// </summary>
    /// <param name="articleId">The unique article identifier.</param>
    /// <param name="tag">The tag to add.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    async Task AddTagAsync(string articleId, string tag, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(articleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);

        IArticleRecord? article = await GetByIdAsync(articleId, cancellationToken).ConfigureAwait(false);
        if (article is null || article.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        await UpdateAsync(
            new Article(article.ArticleId, article.Title, article.RawText, article.CreatedAt, article.Tags.Append(tag.Trim())),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Removes a tag from every article that uses it.
    /// </summary>
    /// <param name="tag">The tag to remove.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    async Task DeleteTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);

        IReadOnlyList<IArticleRecord> articles = await SearchAsync(tag: tag, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        foreach (IArticleRecord article in articles)
        {
            string[] tags = article.Tags
                .Where(current => !string.Equals(current, tag, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            await UpdateAsync(
                new Article(article.ArticleId, article.Title, article.RawText, article.CreatedAt, tags),
                cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Renames a tag on every article that uses it.
    /// </summary>
    /// <param name="oldTag">The existing tag value.</param>
    /// <param name="newTag">The replacement tag value.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    async Task RenameTagAsync(string oldTag, string newTag, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(oldTag);
        ArgumentException.ThrowIfNullOrWhiteSpace(newTag);

        IReadOnlyList<IArticleRecord> articles = await SearchAsync(tag: oldTag, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        foreach (IArticleRecord article in articles)
        {
            string[] tags = article.Tags
                .Select(tag => string.Equals(tag, oldTag, StringComparison.OrdinalIgnoreCase) ? newTag.Trim() : tag)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            await UpdateAsync(
                new Article(article.ArticleId, article.Title, article.RawText, article.CreatedAt, tags),
                cancellationToken).ConfigureAwait(false);
        }
    }
}
