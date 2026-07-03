using TypingCore.Models;

namespace TypingCore.Parsing;

/// <summary>
/// Builds normalized article layout data from raw article text.
/// </summary>
/// <remarks>
/// Implementations may be stateless and are not required to keep mutable shared state.
/// </remarks>
public interface IArticleTextLayoutBuilder
{
    /// <summary>
    /// Builds a normalized layout snapshot from raw article text.
    /// </summary>
    /// <param name="rawText">The raw article text.</param>
    /// <returns>The normalized layout snapshot.</returns>
    ArticleTextLayout Build(string rawText);
}