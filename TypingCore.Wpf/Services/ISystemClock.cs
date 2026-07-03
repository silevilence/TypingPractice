namespace TypingCore.Wpf.Services;

/// <summary>
/// Supplies the current clock value for UI workflows.
/// </summary>
public interface ISystemClock
{
    /// <summary>
    /// Gets the current UTC timestamp.
    /// </summary>
    DateTimeOffset UtcNow { get; }
}