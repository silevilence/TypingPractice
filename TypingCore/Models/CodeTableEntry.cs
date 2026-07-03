namespace TypingCore.Models;

/// <summary>
/// Represents a single code-table entry.
/// </summary>
/// <remarks>
/// Instances copy candidate inputs on construction and are safe for concurrent reads.
/// </remarks>
public sealed record CodeTableEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CodeTableEntry"/> class.
    /// </summary>
    /// <param name="code">The input code.</param>
    /// <param name="candidates">The candidate characters or words for the code.</param>
    /// <param name="priority">The priority used to order this entry.</param>
    public CodeTableEntry(string code, IEnumerable<string> candidates, int priority)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentNullException.ThrowIfNull(candidates);

        Code = code;
        Candidates = candidates.ToArray();
        Priority = priority;
    }

    /// <summary>
    /// Gets the input code.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the candidate characters or words for the code.
    /// </summary>
    public IReadOnlyList<string> Candidates { get; }

    /// <summary>
    /// Gets the priority used to order this entry.
    /// </summary>
    public int Priority { get; }
}