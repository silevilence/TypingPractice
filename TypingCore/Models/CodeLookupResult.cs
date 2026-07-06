using TypingCore.Abstractions;

namespace TypingCore.Models;

/// <summary>
/// Represents immutable candidate codes for source text at the current typing position.
/// </summary>
/// <remarks>
/// Instances copy candidate inputs on construction and are safe for concurrent reads.
/// </remarks>
public sealed record CodeLookupResult : ICodeLookupResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CodeLookupResult"/> class.
    /// </summary>
    /// <param name="sourceText">The source text resolved by the code table.</param>
    /// <param name="candidateCodes">The candidate input codes in priority order.</param>
    public CodeLookupResult(string sourceText, IEnumerable<string> candidateCodes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceText);
        ArgumentNullException.ThrowIfNull(candidateCodes);

        SourceText = sourceText;
        CandidateCodes = candidateCodes.ToArray();
    }

    /// <inheritdoc />
    public string SourceText { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> CandidateCodes { get; }
}
