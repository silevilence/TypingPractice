# 阶段二：数据模型与内部格式设计

## 目标

完成 TypingCore 内部的纯数据模型定稿，并定义文章内部格式与最小分字契约，为后续 SQLite 持久化、文章导入解析、打字状态机和统计引擎提供稳定的数据边界。

## 范围

本阶段包含以下内容：

- 在 TypingCore.Models 中定义文章、文章字符、会话记录、按键记录、统计快照、码表条目与码表容器等纯数据模型。
- 让适合的模型直接实现现有只读边界接口，以减少后续阶段的映射代码。
- 在 TypingCore.Parsing 中定义文章内部格式承载类型与最小分字服务契约。
- 明确文章分字的最小规范化规则、中英文混排规则、宽度分类规则。
- 为上述模型与分字规则补充单元测试。

本阶段不包含以下内容：

- SQLite 仓储实现与数据库表结构设计。
- TXT 导入、编码识别、文本清洗等完整导入流程。
- 打字状态机、IME 事件处理、实时统计计算实现。
- WPF 渲染、布局或界面交互逻辑。

## 设计选择

### 方案选择

采用“接口对齐型模型 + 最小内部格式契约”方案。

原因：

- 现有 Abstractions 已经定义了文章记录、会话记录、统计快照和码表查询结果的边界，阶段二继续补齐对应模型最自然。
- 让 Article、TypingSessionRecord、SessionStatistics 等直接实现只读接口，可以减少阶段三仓储实现和阶段五、六引擎输出时的重复适配。
- 文章分字结果不应直接塞进 Article 本体，否则原始仓储记录和解析后的派生表示会混在一起，影响后续持久化与缓存边界。

不采用以下方案：

- 纯内部模型与接口完全分离：会引入当前没有收益的 adapter 层。
- 提前做完整文章导入与清洗实现：会侵入阶段四职责。

## 分层与落点

- TypingCore.Models 负责承载纯数据模型与只读快照实现。
- TypingCore.Parsing 负责承载文章内部格式与最小分字契约。
- TypingCore.Abstractions 保持跨平台边界定位，只做必要扩展，不承载 Core 内部流程型服务。

因此，文章分字服务契约不放进 Abstractions，而放在 Parsing 命名空间中，作为 Core 内部领域服务接口存在。

## 模型设计

### 文章模型

#### Article

用途：仓储边界中的原始文章记录。

字段：

- ArticleId
- Title
- RawText
- CreatedAt
- Tags

约束：

- 实现 IArticleRecord。
- 作为不可变模型存在。
- 不承载分字结果或派生布局信息。

#### ArticleChar

用途：文章内部逐字符单元，用于后续逐字渲染与逐字比对。

字段：

- Value：当前字符。
- Index：在规范化文本中的零基索引。
- IsPunctuation
- IsWhitespace
- IsLineBreak
- WidthKind

约束：

- 一个字符对应一个 ArticleChar。
- 不做英文单词级聚合。

#### CharacterWidthKind

用途：表达字符显示宽度分类。

枚举值：

- HalfWidth
- FullWidth

阶段二不引入更细的 Unicode East Asian Width 分类，避免过早复杂化。

#### ArticleTextLayout

用途：承载文章内部格式快照。

字段：

- NormalizedText：完成最小规范化后的文本。
- Characters：按顺序排列的 ArticleChar 只读集合。

约束：

- 与 Article 分离，避免原始记录承担派生缓存职责。
- 作为 Parsing 结果的稳定输出类型。

### 会话与统计模型

#### TypingSessionRecord

用途：仓储边界中的会话记录。

字段：

- SessionId
- ArticleId
- StartedAt
- EndedAt

约束：

- 实现 ISessionRecord。
- 命名为 TypingSessionRecord，而不是 TypingSession，避免与后续行为型接口 ITypingSession 混淆。

#### KeystrokeRecord

用途：单次按键记录的纯数据表示。

字段：

- Timestamp
- Key
- IsFromIme
- ImeCommitText
- IsBackspace
- IsCommitted

说明：

- IsCommitted 为只读计算属性，用于表达该按键是否形成了可计入的上屏提交结果。
- 阶段二只定义结构，不定义统计口径，也不区分 IME 组合态内部更多细节。

#### SessionStatistics

用途：会话统计快照。

字段：

- KeystrokesPerMinute
- CharactersPerMinute
- WordsPerMinute
- AverageCodeLength
- BackspaceCount
- ErrorRate
- Elapsed

约束：

- 实现 IStatisticsSnapshot。
- 作为不可变快照存在。

接口变更：

- IStatisticsSnapshot 需要补充 WordsPerMinute，以对齐路线图中 CPM/WPM 并存的正式口径。

### 码表模型

#### CodeTableEntry

用途：码表中的单条编码记录。

字段：

- Code
- Candidates
- Priority

#### CodeTable

用途：码表容器。

字段：

- Name
- Source
- LoadedAt
- Entries

说明：

- CodeTableEntry 与 CodeTable 属于内部数据模型。
- ICodeLookupResult 继续表示查询结果边界，不与内部码表容器合并。

## 文章内部格式与分字规则

### 最小规范化规则

- 将 `\r\n` 与 `\r` 统一转换为 `\n`。
- 除换行统一外，原始文本其余内容保持不变。
- 本阶段不做去空行、裁剪空白、替换制表符或编码清洗。

### 分字规则

- 内部格式按逐字符单元建模，不按英文单词、数字串或词组聚合。
- 中文、英文字母、数字、标点、空格、制表符、换行都各自产生一个 ArticleChar。
- Index 基于规范化后的文本顺序计算。

### 分类规则

- IsWhitespace 对空格、制表符等空白字符成立。
- IsLineBreak 只对 `\n` 成立。
- IsPunctuation 使用 Unicode 类别判断，而不是手写标点表，以统一处理中英文标点。

### 宽度规则

- ASCII 字母、数字、半角符号、空格按 HalfWidth。
- CJK 文字与全角符号按 FullWidth。
- 阶段二只输出 HalfWidth 与 FullWidth 两类，不提前处理 Ambiguous 或 Neutral 的更细粒度差异。

## Parsing 契约

新增最小内部解析契约：IArticleTextLayoutBuilder。

职责：

- 输入原始文章文本。
- 执行最小规范化。
- 输出 ArticleTextLayout。

放置位置：

- TypingCore.Parsing 命名空间。

不放入 Abstractions 的原因：

- 它不是前端平台边界。
- 它属于 Core 内部领域服务，后续仍可能随导入流程演进。

## 测试策略

阶段二测试只验证模型和内部格式，不提前验证状态机、IME 事件流或统计公式。

### 模型契约测试

- 验证 Article 实现 IArticleRecord。
- 验证 TypingSessionRecord 实现 ISessionRecord。
- 验证 SessionStatistics 实现 IStatisticsSnapshot，并包含 WordsPerMinute。

### 分字规则测试

至少覆盖以下样例：

- 纯中文。
- 纯 ASCII。
- 中英混排。
- 中英文标点。
- 空格与 Tab。
- CRLF、CR、LF 三种换行输入。

验证点包括：

- NormalizedText 是否正确。
- Characters 数量与顺序是否正确。
- IsWhitespace、IsLineBreak、IsPunctuation 是否符合规则。
- WidthKind 是否按最小规则分类。

### 码表与统计模型测试

- 验证 CodeTableEntry 与 CodeTable 的字段承载行为。
- 验证 KeystrokeRecord 的 IsCommitted 计算语义符合设计。

## 对现有接口的影响

本阶段只做一个必要的公开契约调整：

- IStatisticsSnapshot 增加 WordsPerMinute。

其余接口保持不动：

- IArticleRepository 不变。
- ISessionRepository 不变。
- IKeyInputEvent 不变。
- ICodeTableProvider 不变。

## 已知边界

- 本阶段只建立最小可用的内部格式，不意味着已经完成完整文章解析模块。
- 宽度分类规则当前为后续渲染服务，若未来需要更精细 Unicode 处理，应在阶段四或阶段九扩展，而不是在阶段二过度设计。
- KeystrokeRecord 当前只表达结构，不对“IME 组合过程中的未上屏编辑”做最终统计语义承诺，该语义应在阶段五和阶段六收敛。