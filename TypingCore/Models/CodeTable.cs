namespace TypingCore.Models;

/// <summary>
/// Represents an immutable code table and its entries.
/// </summary>
/// <remarks>
/// Instances copy entry inputs on construction and are safe for concurrent reads.
/// </remarks>
public sealed record CodeTable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CodeTable"/> class.
    /// </summary>
    /// <param name="name">The display name of the code table.</param>
    /// <param name="source">The source description for the code table.</param>
    /// <param name="loadedAt">The time when the code table was loaded.</param>
    /// <param name="entries">The entries contained in the code table.</param>
    public CodeTable(
        string name,
        string source,
        DateTimeOffset loadedAt,
        IEnumerable<CodeTableEntry> entries)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentNullException.ThrowIfNull(entries);

        Name = name;
        Source = source;
        LoadedAt = loadedAt;
        Entries = entries.ToArray();
    }

    /// <summary>
    /// Gets the display name of the code table.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the source description for the code table.
    /// </summary>
    public string Source { get; }

    /// <summary>
    /// Gets the time when the code table was loaded.
    /// </summary>
    public DateTimeOffset LoadedAt { get; }

    /// <summary>
    /// Gets the entries contained in the code table.
    /// </summary>
    public IReadOnlyList<CodeTableEntry> Entries { get; }
}