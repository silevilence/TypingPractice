namespace TypingCore.Models;

/// <summary>
/// Represents normalized article text together with per-character layout metadata.
/// </summary>
/// <remarks>
/// Instances copy character inputs on construction and are safe for concurrent reads.
/// </remarks>
public sealed record ArticleTextLayout
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArticleTextLayout"/> class.
    /// </summary>
    /// <param name="normalizedText">The normalized article text.</param>
    /// <param name="characters">The per-character layout metadata.</param>
    public ArticleTextLayout(string normalizedText, IEnumerable<ArticleChar> characters)
    {
        ArgumentNullException.ThrowIfNull(normalizedText);
        ArgumentNullException.ThrowIfNull(characters);

        NormalizedText = normalizedText;
        Characters = characters.ToArray();
    }

    /// <summary>
    /// Gets the normalized article text.
    /// </summary>
    public string NormalizedText { get; }

    /// <summary>
    /// Gets the per-character layout metadata.
    /// </summary>
    public IReadOnlyList<ArticleChar> Characters { get; }
}