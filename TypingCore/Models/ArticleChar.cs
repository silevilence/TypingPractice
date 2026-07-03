namespace TypingCore.Models;

/// <summary>
/// Represents a single normalized character in article layout output.
/// </summary>
/// <remarks>
/// Instances are immutable value snapshots and are safe for concurrent reads.
/// </remarks>
public sealed record ArticleChar
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArticleChar"/> class.
    /// </summary>
    /// <param name="value">The character value.</param>
    /// <param name="index">The zero-based index in normalized text.</param>
    /// <param name="characterKind">The semantic character classification.</param>
    /// <param name="widthKind">The display width classification.</param>
    public ArticleChar(
        char value,
        int index,
        ArticleCharacterKind characterKind,
        CharacterWidthKind widthKind)
    {
        Value = value;
        Index = index;
        CharacterKind = characterKind;
        WidthKind = widthKind;
    }

    /// <summary>
    /// Gets the character value.
    /// </summary>
    public char Value { get; }

    /// <summary>
    /// Gets the zero-based index in normalized text.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Gets a value indicating whether the character is punctuation.
    /// </summary>
    public bool IsPunctuation => CharacterKind == ArticleCharacterKind.Punctuation;

    /// <summary>
    /// Gets a value indicating whether the character is whitespace other than a line break.
    /// </summary>
    public bool IsWhitespace => CharacterKind == ArticleCharacterKind.Whitespace;

    /// <summary>
    /// Gets a value indicating whether the character is a normalized line break.
    /// </summary>
    public bool IsLineBreak => CharacterKind == ArticleCharacterKind.LineBreak;

    /// <summary>
    /// Gets the semantic character classification.
    /// </summary>
    public ArticleCharacterKind CharacterKind { get; }

    /// <summary>
    /// Gets the display width classification.
    /// </summary>
    public CharacterWidthKind WidthKind { get; }
}