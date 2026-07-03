namespace TypingCore.Abstractions;

/// <summary>
/// Represents a code-table lookup result for a piece of source text.
/// </summary>
/// <remarks>
/// Implementations should be immutable snapshots and safe for concurrent reads.
/// </remarks>
public interface ICodeLookupResult
{
    /// <summary>
    /// Gets the source text that was resolved.
    /// </summary>
    string SourceText { get; }

    /// <summary>
    /// Gets the candidate codes returned for the source text.
    /// </summary>
    IReadOnlyList<string> CandidateCodes { get; }
}