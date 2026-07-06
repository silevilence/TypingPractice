using TypingCore.Models;

namespace TypingCore.Abstractions;

/// <summary>
/// Parses text code tables into immutable core models.
/// </summary>
/// <remarks>
/// Implementations are expected to be stateless and safe for concurrent use.
/// </remarks>
public interface ICodeTableParser
{
    /// <summary>
    /// Parses code-table text.
    /// </summary>
    /// <param name="name">The display name of the code table.</param>
    /// <param name="source">The source description or file path.</param>
    /// <param name="content">The code-table text.</param>
    /// <param name="loadedAt">The import timestamp.</param>
    /// <returns>The parsed code table.</returns>
    CodeTable Parse(string name, string source, string content, DateTimeOffset loadedAt);

    /// <summary>
    /// Reads and parses a code-table file.
    /// </summary>
    /// <param name="filePath">The file to import.</param>
    /// <param name="loadedAt">The import timestamp.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>The parsed code table.</returns>
    Task<CodeTable> ImportFromFileAsync(
        string filePath,
        DateTimeOffset loadedAt,
        CancellationToken cancellationToken = default);
}
