using TypingCore.Abstractions;
using TypingCore.Models;

namespace TypingCore.Tests.Models;

public class KeystrokeAndCodeTableTests
{
    [Fact]
    public void KeystrokeRecord_marks_commit_when_commit_text_exists()
    {
        KeystrokeRecord imeCommit = new(
            new DateTimeOffset(2026, 7, 3, 9, 0, 0, TimeSpan.Zero),
            KeyInputKey.Character,
            true,
            "你",
            false);

        KeystrokeRecord backspace = new(
            new DateTimeOffset(2026, 7, 3, 9, 0, 1, TimeSpan.Zero),
            KeyInputKey.Backspace,
            false,
            null,
            true);

        Assert.True(imeCommit.IsCommitted);
        Assert.False(backspace.IsCommitted);
    }

    [Fact]
    public void CodeTable_preserves_entries_and_candidates()
    {
        CodeTable table = new(
            "小鹤",
            "built-in",
            new DateTimeOffset(2026, 7, 3, 9, 15, 0, TimeSpan.Zero),
            new[]
            {
                new CodeTableEntry("ni", new[] { "你", "尼" }, 1),
                new CodeTableEntry("hao", new[] { "好" }, 2),
            });

        Assert.Equal("小鹤", table.Name);
        Assert.Equal("built-in", table.Source);
        Assert.Equal(2, table.Entries.Count);
        Assert.Equal("ni", table.Entries[0].Code);
        Assert.Equal(new[] { "你", "尼" }, table.Entries[0].Candidates);
        Assert.Equal(1, table.Entries[0].Priority);
    }
}