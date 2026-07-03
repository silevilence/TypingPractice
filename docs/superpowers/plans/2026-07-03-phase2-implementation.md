# 阶段二数据模型与内部格式设计 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 TypingCore 中落地阶段二的数据模型、最小文章内部格式与分字契约，并用测试锁定这些边界。

**Architecture:** Models 目录承载不可变数据模型与接口实现；Parsing 目录承载文章布局契约与最小分字实现；Tests 先写失败用例，再逐步补齐模型、布局和路线图更新，最后用项目级测试与构建验证收口。

**Tech Stack:** C# 12, .NET 8, xUnit

---

## File Plan

- Create: `TypingCore/Models/Article.cs` - 文章仓储记录实现。
- Create: `TypingCore/Models/ArticleChar.cs` - 逐字符内部模型。
- Create: `TypingCore/Models/CharacterWidthKind.cs` - 字符宽度分类枚举。
- Create: `TypingCore/Models/ArticleTextLayout.cs` - 文章内部格式快照。
- Create: `TypingCore/Models/TypingSessionRecord.cs` - 会话仓储记录实现。
- Create: `TypingCore/Models/KeystrokeRecord.cs` - 单次按键记录模型。
- Create: `TypingCore/Models/SessionStatistics.cs` - 统计快照实现。
- Create: `TypingCore/Models/CodeTableEntry.cs` - 码表条目模型。
- Create: `TypingCore/Models/CodeTable.cs` - 码表容器模型。
- Modify: `TypingCore/Abstractions/IStatisticsSnapshot.cs` - 增加 WordsPerMinute。
- Create: `TypingCore/Parsing/IArticleTextLayoutBuilder.cs` - Core 内部分字契约。
- Create: `TypingCore/Parsing/ArticleTextLayoutBuilder.cs` - 最小分字实现。
- Create: `TypingCore.Tests/Models/ArticleAndSessionModelTests.cs` - Article 与 TypingSessionRecord 契约测试。
- Create: `TypingCore.Tests/Models/SessionStatisticsTests.cs` - 统计快照接口与字段测试。
- Create: `TypingCore.Tests/Models/KeystrokeAndCodeTableTests.cs` - 按键记录与码表模型测试。
- Create: `TypingCore.Tests/Parsing/ArticleTextLayoutBuilderTests.cs` - 换行规范化、字符分类、宽度分类测试。
- Modify: `ROADMAP.md` - 勾选阶段二及其子项。

### Task 1: 先写模型契约失败测试

**Files:**
- Create: `TypingCore.Tests/Models/ArticleAndSessionModelTests.cs`
- Create: `TypingCore.Tests/Models/SessionStatisticsTests.cs`

- [ ] **Step 1: 写 Article 与 TypingSessionRecord 的失败测试**

在 `TypingCore.Tests/Models/ArticleAndSessionModelTests.cs` 写入：

```csharp
using TypingCore.Abstractions;
using TypingCore.Models;

namespace TypingCore.Tests.Models;

public class ArticleAndSessionModelTests
{
    [Fact]
    public void Article_implements_article_record_contract()
    {
        DateTimeOffset createdAt = new(2026, 7, 3, 8, 0, 0, TimeSpan.Zero);

        Article article = new(
            "article-1",
            "示例文章",
            "中A",
            createdAt,
            new[] { "练习", "阶段二" });

        IArticleRecord record = article;

        Assert.Equal("article-1", record.ArticleId);
        Assert.Equal("示例文章", record.Title);
        Assert.Equal("中A", record.RawText);
        Assert.Equal(createdAt, record.CreatedAt);
        Assert.Equal(new[] { "练习", "阶段二" }, record.Tags);
    }

    [Fact]
    public void TypingSessionRecord_implements_session_record_contract()
    {
        DateTimeOffset startedAt = new(2026, 7, 3, 8, 30, 0, TimeSpan.Zero);
        DateTimeOffset endedAt = startedAt.AddMinutes(3);

        TypingSessionRecord session = new(
            "session-1",
            "article-1",
            startedAt,
            endedAt);

        ISessionRecord record = session;

        Assert.Equal("session-1", record.SessionId);
        Assert.Equal("article-1", record.ArticleId);
        Assert.Equal(startedAt, record.StartedAt);
        Assert.Equal(endedAt, record.EndedAt);
    }
}
```

- [ ] **Step 2: 写 SessionStatistics 与 WordsPerMinute 的失败测试**

在 `TypingCore.Tests/Models/SessionStatisticsTests.cs` 写入：

```csharp
using TypingCore.Abstractions;
using TypingCore.Models;

namespace TypingCore.Tests.Models;

public class SessionStatisticsTests
{
    [Fact]
    public void SessionStatistics_exposes_cpm_and_wpm_through_contract()
    {
        SessionStatistics statistics = new(
            320,
            260,
            52,
            4.1,
            3,
            0.04,
            TimeSpan.FromSeconds(95));

        IStatisticsSnapshot snapshot = statistics;

        Assert.Equal(320, snapshot.KeystrokesPerMinute);
        Assert.Equal(260, snapshot.CharactersPerMinute);
        Assert.Equal(52, snapshot.WordsPerMinute);
        Assert.Equal(4.1, snapshot.AverageCodeLength);
        Assert.Equal(3, snapshot.BackspaceCount);
        Assert.Equal(0.04, snapshot.ErrorRate);
        Assert.Equal(TimeSpan.FromSeconds(95), snapshot.Elapsed);
    }
}
```

- [ ] **Step 3: 运行测试确认红灯**

Run: `dotnet test TypingCore.Tests/TypingCore.Tests.csproj`
Expected: 因 `TypingCore.Models` 下类型不存在，且 `IStatisticsSnapshot` 尚无 `WordsPerMinute` 而失败

- [ ] **Step 4: 提交测试脚手架**

```bash
git add TypingCore.Tests/Models/ArticleAndSessionModelTests.cs TypingCore.Tests/Models/SessionStatisticsTests.cs
git commit -m "🧪 test(core): add phase2 model contract tests"
```

### Task 2: 实现文章、会话与统计模型

**Files:**
- Modify: `TypingCore/Abstractions/IStatisticsSnapshot.cs`
- Create: `TypingCore/Models/Article.cs`
- Create: `TypingCore/Models/TypingSessionRecord.cs`
- Create: `TypingCore/Models/SessionStatistics.cs`

- [ ] **Step 1: 扩展统计快照接口**

将 `TypingCore/Abstractions/IStatisticsSnapshot.cs` 中的接口改为：

```csharp
namespace TypingCore.Abstractions;

/// <summary>
/// Represents a read-only statistics snapshot for a typing session.
/// </summary>
/// <remarks>
/// Implementations should be immutable snapshot objects and are expected to be safe for concurrent reads.
/// </remarks>
public interface IStatisticsSnapshot
{
    /// <summary>
    /// Gets the current keystrokes per minute value.
    /// </summary>
    double KeystrokesPerMinute { get; }

    /// <summary>
    /// Gets the current committed characters per minute value.
    /// </summary>
    double CharactersPerMinute { get; }

    /// <summary>
    /// Gets the current words per minute value.
    /// </summary>
    double WordsPerMinute { get; }

    /// <summary>
    /// Gets the current average code length.
    /// </summary>
    double AverageCodeLength { get; }

    /// <summary>
    /// Gets the number of backspace actions counted for the session.
    /// </summary>
    int BackspaceCount { get; }

    /// <summary>
    /// Gets the current error rate, expressed as a value between 0 and 1.
    /// </summary>
    double ErrorRate { get; }

    /// <summary>
    /// Gets the elapsed duration for the session.
    /// </summary>
    TimeSpan Elapsed { get; }
}
```

- [ ] **Step 2: 实现 Article 模型**

创建 `TypingCore/Models/Article.cs`：

```csharp
using TypingCore.Abstractions;

namespace TypingCore.Models;

/// <summary>
/// Represents an immutable article record used by repository and parsing workflows.
/// </summary>
/// <remarks>
/// Instances are immutable and safe for concurrent reads.
/// </remarks>
public sealed record Article(
    string ArticleId,
    string Title,
    string RawText,
    DateTimeOffset CreatedAt,
    IReadOnlyCollection<string> Tags) : IArticleRecord;
```

- [ ] **Step 3: 实现 TypingSessionRecord 与 SessionStatistics**

创建 `TypingCore/Models/TypingSessionRecord.cs`：

```csharp
using TypingCore.Abstractions;

namespace TypingCore.Models;

/// <summary>
/// Represents an immutable persisted typing session record.
/// </summary>
/// <remarks>
/// Instances are immutable and safe for concurrent reads.
/// </remarks>
public sealed record TypingSessionRecord(
    string SessionId,
    string ArticleId,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt) : ISessionRecord;
```

创建 `TypingCore/Models/SessionStatistics.cs`：

```csharp
using TypingCore.Abstractions;

namespace TypingCore.Models;

/// <summary>
/// Represents an immutable statistics snapshot for a typing session.
/// </summary>
/// <remarks>
/// Instances are immutable and safe for concurrent reads.
/// </remarks>
public sealed record SessionStatistics(
    double KeystrokesPerMinute,
    double CharactersPerMinute,
    double WordsPerMinute,
    double AverageCodeLength,
    int BackspaceCount,
    double ErrorRate,
    TimeSpan Elapsed) : IStatisticsSnapshot;
```

- [ ] **Step 4: 运行模型测试确认转绿**

Run: `dotnet test TypingCore.Tests/TypingCore.Tests.csproj --filter "FullyQualifiedName~TypingCore.Tests.Models.ArticleAndSessionModelTests|FullyQualifiedName~TypingCore.Tests.Models.SessionStatisticsTests"`
Expected: 这两个测试类全部通过

- [ ] **Step 5: 提交模型实现**

```bash
git add TypingCore/Abstractions/IStatisticsSnapshot.cs TypingCore/Models/Article.cs TypingCore/Models/TypingSessionRecord.cs TypingCore/Models/SessionStatistics.cs
git commit -m "✨ feat(core): add article session and statistics models"
```

### Task 3: 先写按键记录与码表模型失败测试

**Files:**
- Create: `TypingCore.Tests/Models/KeystrokeAndCodeTableTests.cs`

- [ ] **Step 1: 写 KeystrokeRecord 与码表模型的失败测试**

在 `TypingCore.Tests/Models/KeystrokeAndCodeTableTests.cs` 写入：

```csharp
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
```

- [ ] **Step 2: 运行测试确认红灯**

Run: `dotnet test TypingCore.Tests/TypingCore.Tests.csproj --filter "FullyQualifiedName~TypingCore.Tests.Models.KeystrokeAndCodeTableTests"`
Expected: 因 `KeystrokeRecord`、`CodeTableEntry`、`CodeTable` 尚不存在而失败

- [ ] **Step 3: 提交测试脚手架**

```bash
git add TypingCore.Tests/Models/KeystrokeAndCodeTableTests.cs
git commit -m "🧪 test(core): add phase2 keystroke and code table tests"
```

### Task 4: 实现按键记录与码表模型

**Files:**
- Create: `TypingCore/Models/KeystrokeRecord.cs`
- Create: `TypingCore/Models/CodeTableEntry.cs`
- Create: `TypingCore/Models/CodeTable.cs`

- [ ] **Step 1: 实现 KeystrokeRecord**

创建 `TypingCore/Models/KeystrokeRecord.cs`：

```csharp
using TypingCore.Abstractions;

namespace TypingCore.Models;

/// <summary>
/// Represents an immutable keystroke record captured during a typing session.
/// </summary>
/// <remarks>
/// Instances are immutable and safe for concurrent reads.
/// </remarks>
public sealed record KeystrokeRecord(
    DateTimeOffset Timestamp,
    KeyInputKey Key,
    bool IsFromIme,
    string? ImeCommitText,
    bool IsBackspace)
{
    /// <summary>
    /// Gets a value indicating whether this record contains committed text output.
    /// </summary>
    public bool IsCommitted => !string.IsNullOrEmpty(ImeCommitText);
}
```

- [ ] **Step 2: 实现 CodeTableEntry 与 CodeTable**

创建 `TypingCore/Models/CodeTableEntry.cs`：

```csharp
namespace TypingCore.Models;

/// <summary>
/// Represents a single code-table entry.
/// </summary>
/// <remarks>
/// Instances are immutable and safe for concurrent reads.
/// </remarks>
public sealed record CodeTableEntry(
    string Code,
    IReadOnlyList<string> Candidates,
    int Priority);
```

创建 `TypingCore/Models/CodeTable.cs`：

```csharp
namespace TypingCore.Models;

/// <summary>
/// Represents an immutable code table and its entries.
/// </summary>
/// <remarks>
/// Instances are immutable and safe for concurrent reads.
/// </remarks>
public sealed record CodeTable(
    string Name,
    string Source,
    DateTimeOffset LoadedAt,
    IReadOnlyList<CodeTableEntry> Entries);
```

- [ ] **Step 3: 运行模型测试确认转绿**

Run: `dotnet test TypingCore.Tests/TypingCore.Tests.csproj --filter "FullyQualifiedName~TypingCore.Tests.Models.KeystrokeAndCodeTableTests"`
Expected: `KeystrokeAndCodeTableTests` 通过

- [ ] **Step 4: 提交模型实现**

```bash
git add TypingCore/Models/KeystrokeRecord.cs TypingCore/Models/CodeTableEntry.cs TypingCore/Models/CodeTable.cs
git commit -m "✨ feat(core): add keystroke and code table models"
```

### Task 5: 先写文章布局与分字失败测试

**Files:**
- Create: `TypingCore.Tests/Parsing/ArticleTextLayoutBuilderTests.cs`

- [ ] **Step 1: 写换行规范化与逐字符布局失败测试**

在 `TypingCore.Tests/Parsing/ArticleTextLayoutBuilderTests.cs` 写入：

```csharp
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
}
```

- [ ] **Step 2: 运行测试确认红灯**

Run: `dotnet test TypingCore.Tests/TypingCore.Tests.csproj --filter "FullyQualifiedName~TypingCore.Tests.Parsing.ArticleTextLayoutBuilderTests"`
Expected: 因 `ArticleTextLayout`、`CharacterWidthKind`、`IArticleTextLayoutBuilder`、`ArticleTextLayoutBuilder` 尚不存在而失败

- [ ] **Step 3: 提交测试脚手架**

```bash
git add TypingCore.Tests/Parsing/ArticleTextLayoutBuilderTests.cs
git commit -m "🧪 test(core): add phase2 article layout builder tests"
```

### Task 6: 实现文章内部格式与最小分字器

**Files:**
- Create: `TypingCore/Models/CharacterWidthKind.cs`
- Create: `TypingCore/Models/ArticleChar.cs`
- Create: `TypingCore/Models/ArticleTextLayout.cs`
- Create: `TypingCore/Parsing/IArticleTextLayoutBuilder.cs`
- Create: `TypingCore/Parsing/ArticleTextLayoutBuilder.cs`

- [ ] **Step 1: 实现宽度枚举与文章字符模型**

创建 `TypingCore/Models/CharacterWidthKind.cs`：

```csharp
namespace TypingCore.Models;

/// <summary>
/// Identifies the display width kind for a character in the article layout.
/// </summary>
/// <remarks>
/// Enum values are immutable and thread-safe.
/// </remarks>
public enum CharacterWidthKind
{
    HalfWidth = 0,
    FullWidth = 1,
}
```

创建 `TypingCore/Models/ArticleChar.cs`：

```csharp
namespace TypingCore.Models;

/// <summary>
/// Represents a single normalized character in article layout output.
/// </summary>
/// <remarks>
/// Instances are immutable and safe for concurrent reads.
/// </remarks>
public sealed record ArticleChar(
    char Value,
    int Index,
    bool IsPunctuation,
    bool IsWhitespace,
    bool IsLineBreak,
    CharacterWidthKind WidthKind);
```

- [ ] **Step 2: 实现 ArticleTextLayout 与 Parsing 契约**

创建 `TypingCore/Models/ArticleTextLayout.cs`：

```csharp
namespace TypingCore.Models;

/// <summary>
/// Represents normalized article text together with its per-character layout metadata.
/// </summary>
/// <remarks>
/// Instances are immutable and safe for concurrent reads.
/// </remarks>
public sealed record ArticleTextLayout(
    string NormalizedText,
    IReadOnlyList<ArticleChar> Characters);
```

创建 `TypingCore/Parsing/IArticleTextLayoutBuilder.cs`：

```csharp
using TypingCore.Models;

namespace TypingCore.Parsing;

/// <summary>
/// Builds normalized article layout data from raw article text.
/// </summary>
/// <remarks>
/// Implementations may be stateless and are not required to keep mutable shared state.
/// </remarks>
public interface IArticleTextLayoutBuilder
{
    /// <summary>
    /// Builds a normalized layout snapshot from raw article text.
    /// </summary>
    /// <param name="rawText">The raw article text.</param>
    /// <returns>The normalized layout snapshot.</returns>
    ArticleTextLayout Build(string rawText);
}
```

- [ ] **Step 3: 实现 ArticleTextLayoutBuilder**

创建 `TypingCore/Parsing/ArticleTextLayoutBuilder.cs`：

```csharp
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
```

- [ ] **Step 4: 运行布局测试确认转绿**

Run: `dotnet test TypingCore.Tests/TypingCore.Tests.csproj --filter "FullyQualifiedName~TypingCore.Tests.Parsing.ArticleTextLayoutBuilderTests"`
Expected: `ArticleTextLayoutBuilderTests` 通过

- [ ] **Step 5: 提交布局实现**

```bash
git add TypingCore/Models/CharacterWidthKind.cs TypingCore/Models/ArticleChar.cs TypingCore/Models/ArticleTextLayout.cs TypingCore/Parsing/IArticleTextLayoutBuilder.cs TypingCore/Parsing/ArticleTextLayoutBuilder.cs
git commit -m "✨ feat(core): add article text layout builder"
```

### Task 7: 做最终验证并更新路线图

**Files:**
- Modify: `ROADMAP.md`

- [ ] **Step 1: 运行完整测试项目**

Run: `dotnet test TypingCore.Tests/TypingCore.Tests.csproj`
Expected: TypingCore.Tests 全部通过

- [ ] **Step 2: 运行解决方案构建**

Run: `dotnet build TypingPractice.sln`
Expected: 解决方案构建成功

- [ ] **Step 3: 勾选路线图中的阶段二**

将 `ROADMAP.md` 中阶段二修改为：

```markdown
- [x] **阶段二：数据模型与内部格式设计**
    - [x] 定义文章内部数据模型
        - [x] `Article`：Id、标题、原始文本、创建时间、标签
        - [x] `ArticleChar`：单字模型（字符、索引、是否为标点/空格、宽度类型标记）
        - [x] 文章分字算法（考虑中英文混排、全角半角、换行处理）
    - [x] 定义打字会话与统计数据模型
        - [x] `TypingSession`：会话 Id、关联文章 Id、开始/结束时间
        - [x] `KeystrokeRecord`：单次按键记录（时间戳、键值、是否上屏、是否退格）
        - [x] `SessionStatistics`：键速（KPM）、字速（CPM/WPM）、平均码长、退格次数、错误率、总耗时
    - [x] 定义码表数据模型
        - [x] `CodeTableEntry`：编码、候选字/词列表、优先级
        - [x] `CodeTable`：码表名称、来源、加载时间
```

- [ ] **Step 4: 提交路线图与最终验证结果**

```bash
git add ROADMAP.md
git commit -m "📚 docs(roadmap): mark phase2 complete"
```

## Self-Review Checklist

- 模型命名与规格保持一致：Article、TypingSessionRecord、KeystrokeRecord、SessionStatistics、CodeTableEntry、CodeTable、ArticleTextLayout。
- `IStatisticsSnapshot` 与 `SessionStatistics` 同步包含 `WordsPerMinute`。
- 分字规则只包含最小换行规范化，不额外引入文本清洗。
- 布局测试覆盖中文、ASCII、全角标点、空格、Tab、CRLF/CR/LF。
- 最终必须同时跑 `dotnet test TypingCore.Tests/TypingCore.Tests.csproj` 与 `dotnet build TypingPractice.sln` 后才能声称阶段二完成。