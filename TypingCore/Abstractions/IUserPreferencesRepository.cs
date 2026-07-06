using TypingCore.Models;

namespace TypingCore.Abstractions;

/// <summary>
/// Loads and saves user preferences without exposing platform-specific storage details.
/// </summary>
/// <remarks>
/// Thread-safety is implementation-defined.
/// </remarks>
public interface IUserPreferencesRepository
{
    /// <summary>
    /// Loads persisted preferences or returns application defaults when none exist.
    /// </summary>
    Task<UserPreferences> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists the supplied preferences.
    /// </summary>
    Task SaveAsync(
        UserPreferences preferences,
        CancellationToken cancellationToken = default);
}
