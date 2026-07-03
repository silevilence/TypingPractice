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
    /// <param name="isPunctuation">A value indicating whether the character is punctuation.</param>
    /// <param name="isWhitespace">A value indicating whether the character is whitespace other than a line break.</param>
    /// <param name="isLineBreak">A value indicating whether the character is a normalized line break.</param>
    /// <param name="widthKind">The display width classification.</param>
    public ArticleChar(
        char value,
        int index,
        bool isPunctuation,
        bool isWhitespace,
        bool isLineBreak,
        CharacterWidthKind widthKind)
    {
        Value = value;
        Index = index;
        IsPunctuation = isPunctuation;
        IsWhitespace = isWhitespace;
        IsLineBreak = isLineBreak;
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
    public bool IsPunctuation { get; }

    /// <summary>
    /// Gets a value indicating whether the character is whitespace other than a line break.
    /// </summary>
    public bool IsWhitespace { get; }

    /// <summary>
    /// Gets a value indicating whether the character is a normalized line break.
    /// </summary>
    public bool IsLineBreak { get; }

    /// <summary>
    /// Gets the display width classification.
    /// </summary>
    public CharacterWidthKind WidthKind { get; }
}