using TypingCore.Abstractions;
using TypingCore.Engine;
using TypingCore.Models;

namespace TypingCore.Tests.Engine;

public sealed class CodeTableProviderTests
{
    [Fact]
    public void Lookup_returns_longest_current_prefix_and_weighted_codes()
    {
        CodeTableProvider provider = new();
        provider.Load(new CodeTable(
            "五笔86",
            "memory",
            new DateTimeOffset(2026, 7, 6, 8, 0, 0, TimeSpan.Zero),
            new[]
            {
                new CodeTableEntry("aa", new[] { "工" }, 10),
                new CodeTableEntry("a", new[] { "工" }, 999),
                new CodeTableEntry("ggtt", new[] { "工人" }, 500),
                new CodeTableEntry("www", new[] { "人" }, 100),
            }));

        IReadOnlyList<ICodeLookupResult> results = provider.Lookup("工人练习");

        Assert.Equal(new[] { "工人", "工" }, results.Select(result => result.SourceText));
        Assert.Equal(new[] { "ggtt" }, results[0].CandidateCodes);
        Assert.Equal(new[] { "a", "aa" }, results[1].CandidateCodes);
    }

    [Fact]
    public void Clear_removes_active_index()
    {
        CodeTableProvider provider = new();
        provider.Load(new CodeTable(
            "五笔86",
            "memory",
            new DateTimeOffset(2026, 7, 6, 8, 0, 0, TimeSpan.Zero),
            new[] { new CodeTableEntry("a", new[] { "工" }, 1) }));

        provider.Clear();

        Assert.Empty(provider.Lookup("工"));
        Assert.Null(provider.CurrentTable);
    }
}
