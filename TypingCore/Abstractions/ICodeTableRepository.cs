using TypingCore.Models;

namespace TypingCore.Abstractions;

/// <summary>
/// Persists imported code tables and the active table selection.
/// </summary>
/// <remarks>
/// Implementations define their own concurrency guarantees.
/// </remarks>
public interface ICodeTableRepository
{
    /// <summary>
    /// Gets all persisted code tables.
    /// </summary>
    Task<IReadOnlyList<CodeTable>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports and persists a code-table file.
    /// </summary>
    Task<CodeTable> ImportAsync(
        string sourceFilePath,
        DateTimeOffset loadedAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a persisted code table.
    /// </summary>
    Task DeleteAsync(string storedSource, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the persisted source path of the active code table.
    /// </summary>
    Task<string?> GetActiveSourceAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists the active code-table source path, or clears it when <paramref name="storedSource"/> is null.
    /// </summary>
    Task SetActiveSourceAsync(
        string? storedSource,
        CancellationToken cancellationToken = default);
}
