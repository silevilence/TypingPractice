namespace TypingCore.Abstractions;

/// <summary>
/// Looks up candidate input codes for a piece of source text.
/// </summary>
/// <remarks>
/// Implementations may cache lookup data internally. Thread safety is adapter-defined.
/// </remarks>
public interface ICodeTableProvider
{
    /// <summary>
    /// Looks up candidate codes for the specified text.
    /// </summary>
    /// <param name="text">The source text to resolve.</param>
    /// <param name="maxResults">The maximum number of results to return.</param>
    /// <returns>A read-only list of candidate code results.</returns>
    IReadOnlyList<ICodeLookupResult> Lookup(string text, int maxResults = 5);
}