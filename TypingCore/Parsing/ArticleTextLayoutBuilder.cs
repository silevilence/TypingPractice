using System.Globalization;
using TypingCore.Models;

namespace TypingCore.Parsing;

/// <summary>
/// Converts raw article text into a normalized per-character layout snapshot.
/// </summary>
/// <remarks>
/// Instances are stateless and safe for concurrent use.
/// </remarks>
public sealed class ArticleTextLayoutBuilder : IArticleTextLayoutBuilder
{
    /// <inheritdoc />
    public ArticleTextLayout Build(string rawText)
    {
        ArgumentNullException.ThrowIfNull(rawText);

        string normalizedText = rawText
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal);

        List<ArticleChar> characters = new(normalizedText.Length);

        for (int index = 0; index < normalizedText.Length; index++)
        {
            char value = normalizedText[index];
            bool isLineBreak = value == '\n';
            bool isWhitespace = !isLineBreak && char.IsWhiteSpace(value);

            characters.Add(new ArticleChar(
                value,
                index,
                IsPunctuation(value),
                isWhitespace,
                isLineBreak,
                GetWidthKind(value)));
        }

        return new ArticleTextLayout(normalizedText, characters);
    }

    private static bool IsPunctuation(char value)
    {
        UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(value);

        return category is UnicodeCategory.ConnectorPunctuation
            or UnicodeCategory.DashPunctuation
            or UnicodeCategory.OpenPunctuation
            or UnicodeCategory.ClosePunctuation
            or UnicodeCategory.InitialQuotePunctuation
            or UnicodeCategory.FinalQuotePunctuation
            or UnicodeCategory.OtherPunctuation;
    }

    private static CharacterWidthKind GetWidthKind(char value)
    {
        if (value == '\n' || value == '\t' || value <= '\u007F')
        {
            return CharacterWidthKind.HalfWidth;
        }

        return IsWideCodePoint(value)
            ? CharacterWidthKind.FullWidth
            : CharacterWidthKind.HalfWidth;
    }

    private static bool IsWideCodePoint(char value)
    {
        return value is >= '\u1100' and <= '\u115F'
            or >= '\u2E80' and <= '\uA4CF'
            or >= '\uAC00' and <= '\uD7A3'
            or >= '\uF900' and <= '\uFAFF'
            or >= '\uFE10' and <= '\uFE19'
            or >= '\uFE30' and <= '\uFE6F'
            or >= '\uFF01' and <= '\uFF60'
            or >= '\uFFE0' and <= '\uFFE6';
    }
}