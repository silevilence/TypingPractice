using TypingCore.Models;
using TypingCore.Parsing;

namespace TypingCore.Tests.Parsing;

public class ArticleTextLayoutBuilderTests
{
    private readonly IArticleTextLayoutBuilder builder = new ArticleTextLayoutBuilder();

    [Fact]
    public void Build_normalizes_line_endings_and_preserves_character_order()
    {
        ArticleTextLayout layout = builder.Build("中A\r\nB\rC");

        Assert.Equal("中A\nB\nC", layout.NormalizedText);
        Assert.Equal(layout.NormalizedText.Length, layout.Characters.Count);

        Assert.Collection(
            layout.Characters,
            character =>
            {
                Assert.Equal('中', character.Value);
                Assert.Equal(0, character.Index);
                Assert.Equal(CharacterWidthKind.FullWidth, character.WidthKind);
                Assert.False(character.IsWhitespace);
                Assert.False(character.IsLineBreak);
                Assert.False(character.IsPunctuation);
            },
            character =>
            {
                Assert.Equal('A', character.Value);
                Assert.Equal(1, character.Index);
                Assert.Equal(CharacterWidthKind.HalfWidth, character.WidthKind);
            },
            character =>
            {
                Assert.Equal('\n', character.Value);
                Assert.True(character.IsLineBreak);
                Assert.False(character.IsWhitespace);
                Assert.Equal(CharacterWidthKind.HalfWidth, character.WidthKind);
            },
            character => Assert.Equal('B', character.Value),
            character => Assert.Equal('\n', character.Value),
            character => Assert.Equal('C', character.Value));
    }

    [Fact]
    public void Build_marks_whitespace_punctuation_and_width_kind()
    {
        ArticleTextLayout layout = builder.Build("A， \t!");

        Assert.Equal('，', layout.Characters[1].Value);
        Assert.True(layout.Characters[1].IsPunctuation);
        Assert.Equal(CharacterWidthKind.FullWidth, layout.Characters[1].WidthKind);

        Assert.Equal(' ', layout.Characters[2].Value);
        Assert.True(layout.Characters[2].IsWhitespace);
        Assert.Equal(CharacterWidthKind.HalfWidth, layout.Characters[2].WidthKind);

        Assert.Equal('\t', layout.Characters[3].Value);
        Assert.True(layout.Characters[3].IsWhitespace);
        Assert.False(layout.Characters[3].IsLineBreak);

        Assert.Equal('!', layout.Characters[4].Value);
        Assert.True(layout.Characters[4].IsPunctuation);
        Assert.Equal(CharacterWidthKind.HalfWidth, layout.Characters[4].WidthKind);
    }

    [Fact]
    public void Build_collapses_extra_blank_lines_and_trims_blank_edges()
    {
        ArticleTextLayout layout = builder.Build("\r\n\r\n第一行\r\n\r\n\r\n第二行\r第三行\n\n");

        Assert.Equal("第一行\n\n第二行\n第三行", layout.NormalizedText);
    }

    [Fact]
    public void Build_exposes_character_kind_for_mixed_text()
    {
        ArticleTextLayout layout = builder.Build("中A3， \n下");

        Assert.Equal(ArticleCharacterKind.Cjk, layout.Characters[0].CharacterKind);
        Assert.Equal(ArticleCharacterKind.LatinLetter, layout.Characters[1].CharacterKind);
        Assert.Equal(ArticleCharacterKind.Digit, layout.Characters[2].CharacterKind);
        Assert.Equal(ArticleCharacterKind.Punctuation, layout.Characters[3].CharacterKind);
        Assert.Equal(ArticleCharacterKind.Whitespace, layout.Characters[4].CharacterKind);
        Assert.Equal(ArticleCharacterKind.LineBreak, layout.Characters[5].CharacterKind);
        Assert.Equal(ArticleCharacterKind.Cjk, layout.Characters[6].CharacterKind);
    }
}