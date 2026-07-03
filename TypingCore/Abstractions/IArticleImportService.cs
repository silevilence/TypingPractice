using TypingCore.Models;

namespace TypingCore.Abstractions;

/// <summary>
/// Imports article text into a normalized layout snapshot for storage or rendering workflows.
/// </summary>
/// <remarks>
/// Implementations may be stateless and are expected to be safe for concurrent use.
/// </remarks>
public interface IArticleImportService
{
    /// <summary>
    /// Imports article text that has already been provided by the caller, such as clipboard content.
    /// </summary>
    /// <param name="title">The article title supplied by the caller.</param>
    /// <param name="rawText">The raw article text to normalize.</param>
    /// <returns>The normalized article import result.</returns>
    ArticleImportResult ImportFromText(string title, string rawText);

    /// <summary>
    /// Imports article text from a text file.
    /// </summary>
    /// <param name="filePath">The file path to import.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>The normalized article import result.</returns>
    Task<ArticleImportResult> ImportFromFileAsync(
        string filePath,
        CancellationToken cancellationToken = default);
}