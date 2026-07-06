using TypingCore.Abstractions;
using TypingCore.Models;

namespace TypingCore.Engine;

/// <summary>
/// Builds an in-memory reverse index for the active code table.
/// </summary>
/// <remarks>
/// Loading replaces the complete immutable index atomically. Lookups are safe for concurrent reads.
/// </remarks>
public sealed class CodeTableProvider : ICodeTableProvider
{
    private IndexSnapshot snapshot = IndexSnapshot.Empty;

    /// <summary>
    /// Gets the currently active code table, or <see langword="null"/> when none is loaded.
    /// </summary>
    public CodeTable? CurrentTable => Volatile.Read(ref snapshot).Table;

    /// <summary>
    /// Replaces the active code table and reverse index.
    /// </summary>
    /// <param name="table">The code table to load.</param>
    public void Load(CodeTable table)
    {
        ArgumentNullException.ThrowIfNull(table);

        Dictionary<string, List<WeightedCode>> weightedCodes = new(StringComparer.Ordinal);

        foreach (CodeTableEntry entry in table.Entries)
        {
            foreach (string candidate in entry.Candidates)
            {
                if (string.IsNullOrWhiteSpace(candidate))
                {
                    continue;
                }

                if (!weightedCodes.TryGetValue(candidate, out List<WeightedCode>? codes))
                {
                    codes = [];
                    weightedCodes.Add(candidate, codes);
                }

                codes.Add(new WeightedCode(entry.Code, entry.Priority));
            }
        }

        Dictionary<string, IReadOnlyList<string>> codesByText = weightedCodes.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<string>)pair.Value
                .OrderByDescending(item => item.Weight)
                .ThenBy(item => item.Code.Length)
                .ThenBy(item => item.Code, StringComparer.Ordinal)
                .Select(item => item.Code)
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
            StringComparer.Ordinal);
        int maxSourceLength = codesByText.Count == 0
            ? 0
            : codesByText.Keys.Max(text => text.Length);

        Volatile.Write(ref snapshot, new IndexSnapshot(table, codesByText, maxSourceLength));
    }

    /// <summary>
    /// Removes the active code table and all indexed entries.
    /// </summary>
    public void Clear() => Volatile.Write(ref snapshot, IndexSnapshot.Empty);

    /// <inheritdoc />
    public IReadOnlyList<ICodeLookupResult> Lookup(string text, int maxResults = 5)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (text.Length == 0 || maxResults <= 0)
        {
            return [];
        }

        IndexSnapshot current = Volatile.Read(ref snapshot);
        List<ICodeLookupResult> results = [];
        int maxLength = Math.Min(text.Length, current.MaxSourceLength);

        for (int length = maxLength; length > 0 && results.Count < maxResults; length--)
        {
            string sourceText = text[..length];
            if (current.CodesByText.TryGetValue(sourceText, out IReadOnlyList<string>? codes))
            {
                results.Add(new CodeLookupResult(sourceText, codes));
            }
        }

        return results;
    }

    private sealed record IndexSnapshot(
        CodeTable? Table,
        IReadOnlyDictionary<string, IReadOnlyList<string>> CodesByText,
        int MaxSourceLength)
    {
        public static IndexSnapshot Empty { get; } =
            new(null, new Dictionary<string, IReadOnlyList<string>>(), 0);
    }

    private sealed record WeightedCode(string Code, int Weight);
}
