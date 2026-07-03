namespace TypingCore.Abstractions;

/// <summary>
/// Exposes statistics calculated for a typing session.
/// </summary>
/// <remarks>
/// Implementations may update over time, but callers should treat snapshot retrieval as side-effect free.
/// Thread safety is adapter-defined.
/// </remarks>
public interface IStatisticsProvider
{
    /// <summary>
    /// Gets the latest available statistics snapshot.
    /// </summary>
    IStatisticsSnapshot Current { get; }
}