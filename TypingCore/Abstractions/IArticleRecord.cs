namespace TypingCore.Abstractions;

/// <summary>
/// Represents article data exchanged through repository boundaries.
/// </summary>
/// <remarks>
/// Implementations should be immutable snapshots. Concurrent read safety follows from immutability.
/// </remarks>
public interface IArticleRecord
{
    /// <summary>
    /// Gets the unique article identifier.
    /// </summary>
    string ArticleId { get; }

    /// <summary>
    /// Gets the article title.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets the raw article text.
    /// </summary>
    string RawText { get; }

    /// <summary>
    /// Gets the creation timestamp.
    /// </summary>
    DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// Gets the article tags.
    /// </summary>
    IReadOnlyCollection<string> Tags { get; }
}