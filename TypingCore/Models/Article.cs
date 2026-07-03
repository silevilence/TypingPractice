using TypingCore.Abstractions;

namespace TypingCore.Models;

/// <summary>
/// Represents an immutable article record used by repository and parsing workflows.
/// </summary>
/// <remarks>
/// Instances copy tag inputs on construction and are safe for concurrent reads.
/// </remarks>
public sealed record Article : IArticleRecord
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Article"/> class.
    /// </summary>
    /// <param name="articleId">The unique article identifier.</param>
    /// <param name="title">The article title.</param>
    /// <param name="rawText">The raw article text.</param>
    /// <param name="createdAt">The creation timestamp.</param>
    /// <param name="tags">The article tags.</param>
    public Article(
        string articleId,
        string title,
        string rawText,
        DateTimeOffset createdAt,
        IEnumerable<string> tags)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(articleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(rawText);
        ArgumentNullException.ThrowIfNull(tags);

        ArticleId = articleId;
        Title = title;
        RawText = rawText;
        CreatedAt = createdAt;
        Tags = tags.ToArray();
    }

    /// <inheritdoc />
    public string ArticleId { get; }

    /// <inheritdoc />
    public string Title { get; }

    /// <inheritdoc />
    public string RawText { get; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; }

    /// <inheritdoc />
    public IReadOnlyCollection<string> Tags { get; }
}