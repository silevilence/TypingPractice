using TypingCore.Models;
using TypingCore.Parsing;

namespace TypingCore.Tests.Parsing;

public sealed class CodeTableParserTests
{
    private static readonly DateTimeOffset LoadedAt =
        new(2026, 7, 6, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Parse_ignores_optional_yaml_and_comments_and_reads_tab_rows()
    {
        const string content = """
            ---
            wildcard_key: z
            charset: abcdefghijklmnopqrstuvwxyz
            ---
            # 五笔86 简易示例码表
            a	工	999
            ab	式
            """;

        CodeTable table = new CodeTableParser().Parse(
            "五笔86",
            "memory",
            content,
            LoadedAt);

        Assert.Equal("五笔86", table.Name);
        Assert.Equal(2, table.Entries.Count);
        Assert.Equal("a", table.Entries[0].Code);
        Assert.Equal(new[] { "工" }, table.Entries[0].Candidates);
        Assert.Equal(999, table.Entries[0].Priority);
        Assert.Equal(0, table.Entries[1].Priority);
    }

    [Fact]
    public void Parse_accepts_literal_tab_marker_used_in_documentation_samples()
    {
        const string content = "a<TAB>工<TAB>999";

        CodeTable table = new CodeTableParser().Parse(
            "示例",
            "memory",
            content,
            LoadedAt);

        CodeTableEntry entry = Assert.Single(table.Entries);
        Assert.Equal("a", entry.Code);
        Assert.Equal("工", Assert.Single(entry.Candidates));
        Assert.Equal(999, entry.Priority);
    }

    [Fact]
    public void Parse_throws_with_line_number_for_invalid_rows()
    {
        FormatException exception = Assert.Throws<FormatException>(() =>
            new CodeTableParser().Parse(
                "错误示例",
                "memory",
                "# comment\nmissing-column",
                LoadedAt));

        Assert.Contains("第 2 行", exception.Message);
    }
}
