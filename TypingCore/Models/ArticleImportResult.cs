namespace TypingCore.Models;

/// <summary>
/// Represents the normalized result of importing article text from a file or clipboard source.
/// </summary>
/// <remarks>
/// Instances are immutable snapshots and are safe for concurrent reads.
/// </remarks>
public sealed record ArticleImportResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArticleImportResult"/> class.
    /// </summary>
    /// <param name="title">The imported article title.</param>
    /// <param name="layout">The normalized layout data.</param>
    /// <param name="detectedEncodingName">The detected file encoding name, if the source was a file.</param>
    public ArticleImportResult(
        string title,
        ArticleTextLayout layout,
        string? detectedEncodingName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(layout);

        Title = title;
        Layout = layout;
        DetectedEncodingName = detectedEncodingName;
    }

    /// <summary>
    /// Gets the imported article title.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets the normalized article text.
    /// </summary>
    public string NormalizedText => Layout.NormalizedText;

    /// <summary>
    /// Gets the normalized per-character layout metadata.
    /// </summary>
    public ArticleTextLayout Layout { get; }

    /// <summary>
    /// Gets the detected file encoding name, or <see langword="null"/> for direct text imports.
    /// </summary>
    public string? DetectedEncodingName { get; }
}