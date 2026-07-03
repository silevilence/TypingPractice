namespace TypingCore.Models;

/// <summary>
/// Identifies the semantic kind of a character in article layout output.
/// </summary>
/// <remarks>
/// Enum values are immutable and thread-safe.
/// </remarks>
public enum ArticleCharacterKind
{
    Other = 0,
    Cjk = 1,
    LatinLetter = 2,
    Digit = 3,
    Punctuation = 4,
    Whitespace = 5,
    LineBreak = 6,
}