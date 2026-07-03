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

        string normalizedText = NormalizeText(rawText);

        List<ArticleChar> characters = new(normalizedText.Length);

        for (int index = 0; index < normalizedText.Length; index++)
        {
            char value = normalizedText[index];
            bool isLineBreak = value == '\n';
            bool isWhitespace = !isLineBreak && char.IsWhiteSpace(value);
            bool isPunctuation = IsPunctuation(value);
            ArticleCharacterKind characterKind = GetCharacterKind(value, isPunctuation, isWhitespace, isLineBreak);

            characters.Add(new ArticleChar(
                value,
                index,
                characterKind,
                GetWidthKind(value)));
        }

        return new ArticleTextLayout(normalizedText, characters);
    }

    private static string NormalizeText(string rawText)
    {
        string lineEndingNormalized = rawText
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal);

        string[] lines = lineEndingNormalized.Split('\n');
        List<string> normalizedLines = new(lines.Length);
        bool previousLineWasBlank = true;

        foreach (string line in lines)
        {
            bool isBlankLine = string.IsNullOrWhiteSpace(line);

            if (isBlankLine)
            {
                if (!previousLineWasBlank)
                {
                    normalizedLines.Add(string.Empty);
                }
            }
            else
            {
                normalizedLines.Add(line);
            }

            previousLineWasBlank = isBlankLine;
        }

        while (normalizedLines.Count > 0 && normalizedLines[^1].Length == 0)
        {
            normalizedLines.RemoveAt(normalizedLines.Count - 1);
        }

        return string.Join('\n', normalizedLines);
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

    private static ArticleCharacterKind GetCharacterKind(
        char value,
        bool isPunctuation,
        bool isWhitespace,
        bool isLineBreak)
    {
        if (isLineBreak)
        {
            return ArticleCharacterKind.LineBreak;
        }

        if (isWhitespace)
        {
            return ArticleCharacterKind.Whitespace;
        }

        if (isPunctuation)
        {
            return ArticleCharacterKind.Punctuation;
        }

        if (char.IsDigit(value))
        {
            return ArticleCharacterKind.Digit;
        }

        if (IsCjkCodePoint(value))
        {
            return ArticleCharacterKind.Cjk;
        }

        if (IsLatinLetter(value))
        {
            return ArticleCharacterKind.LatinLetter;
        }

        return ArticleCharacterKind.Other;
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

    private static bool IsLatinLetter(char value)
    {
        return value is >= 'A' and <= 'Z'
            or >= 'a' and <= 'z'
            or >= '\u00C0' and <= '\u024F'
            or >= '\uFF21' and <= '\uFF3A'
            or >= '\uFF41' and <= '\uFF5A';
    }

    private static bool IsCjkCodePoint(char value)
    {
        return value is >= '\u3400' and <= '\u4DBF'
            or >= '\u4E00' and <= '\u9FFF'
            or >= '\uF900' and <= '\uFAFF';
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