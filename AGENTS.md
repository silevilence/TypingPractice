# AGENTS.md

本文件用于约束 AI（含 Claude、Copilot、Cursor 等各类代码生成/协作工具）在本项目中的开发行为。任何 AI 在本仓库内进行代码生成、重构、修复时，**必须先阅读并遵守本文件**，其优先级高于 AI 自身的默认行为习惯。

---

## 0. 项目基本信息（AI 必须牢记的上下文）

- **项目名称**：打字练习软件（类极速跟打器）
- **目标平台**：仅 Windows（暂不考虑其他平台，但架构须为跨平台预留接口）
- **技术栈**：C# 12 / .NET 8.0，前端 WPF，数据库 SQLite（Microsoft.Data.Sqlite 8.0.0），MVVM（CommunityToolkit.Mvvm 8.2.2），图表（LiveCharts2 2.0.4），打包（Velopack 1.2.0）
- **测试框架**：xUnit 2.5.3 + Microsoft.NET.Test.Sdk 17.8.0 + coverlet.collector 6.0.0
- **架构分层**：
  - `TypingCore`：平台无关的核心类库（文章解析、打字状态机、统计计算、码表反查、数据访问）
    - `Abstractions/`：跨平台接口定义（`ITypingSession`、`IKeyInputEvent`、`IStatisticsProvider`、`ICodeTableProvider`、`ICodeTableRepository`、`IUserPreferencesRepository` 等）
    - `Engine/`：打字比对状态机（`TypingSession`）、码表查询与内存索引（`CodeTableProvider`）
    - `Models/`：不可变数据模型（`Article`、`SessionStatistics`、`CodeTable`、`UserPreferences` 等，均为 `record` 类型）
    - `Parsing/`：文章导入与文本规范化（`ArticleImportService`、`ArticleTextLayoutBuilder`）、码表解析（`CodeTableParser`）、编码检测（`TextFileDecoder`）
    - `Persistence/`：数据访问层（`SqliteArticleRepository`、`SqliteSessionRepository`、`FileCodeTableRepository`、`JsonUserPreferencesRepository`）
  - `TypingCore.Wpf`：Windows 前端，只做渲染与 Win32/IME 事件翻译
    - `Views/`：6 个页面（文章库、打字练习、练习结果、历史记录、设置、码表管理）+ 2 个自定义渲染控件
    - `ViewModels/`：MVVM 视图模型（`MainViewModel` 导航，各页面独立 ViewModel）
    - `Services/`：平台服务（`VelopackUpdateService`、`WindowMessageInputTranslator`、`ApplicationThemeManager`、`ClipboardService`、`FileDialogService`）
  - `TypingCore.Tests`：单元测试，目录结构镜像 TypingCore + `Wpf/` 子目录覆盖前端行为
- **核心难点**：IME（输入法）事件处理，`WM_KEYDOWN` 用于键速/退格统计，`WM_IME_CHAR`/`WM_CHAR` 用于上屏字符比对
- **打包发布**：Velopack 1.2.0 + GitHub Actions，按 `v*.*.*` tag 触发自动发布（基础设施已搭建，待端到端验证）
- **当前进度**：阶段一至十四已完成（核心引擎、WPF 前端全部页面、码表、设置、测试），阶段十五（打包与发布）开发中

AI 不得在未经用户明确要求的情况下，擅自变更上述技术选型（例如擅自把 WPF 换成 Avalonia、把 SQLite 换成其他数据库等）。如认为现有选型有问题，应先提出建议并等待用户确认，而不是直接修改。

---

## 1. 代码生成的基本原则

- **禁止占位符**
    - 不允许出现 `// TODO`、`// 其余代码相同`、`...` 等省略写法
    - 每次输出的代码文件必须是完整可编译的
- **禁止臆造 API**
    - 不确定某个 .NET/WPF API 是否存在或签名如何时，必须先说明不确定，而不是编造一个看似合理的方法名
    - 涉及 Win32 互操作（P/Invoke）时，函数签名、常量值必须核对准确，错误的 DllImport 签名会导致难以排查的崩溃
- **禁止跨层污染**
    - `TypingCore` 项目中禁止出现任何 WPF、Win32、System.Windows.* 相关引用
    - `TypingCore.Wpf` 不得绕过 Core 的标准接口直接操作统计/持久化逻辑
    - 如果发现某个功能"顺手"写在了错误的层，必须提示用户并建议移动，而不是默认放着

---

## 2. 架构与接口约束

- **Core 对外接口只能通过标准化类型交互**
    - 所有按键输入必须封装为统一的 `KeyInputEvent`（或后续演进的等价类型），不得让 WPF 层的 `KeyEventArgs` 直接进入 Core
    - Core 输出的状态（比对结果、统计数据）必须是 POCO（不依赖任何 UI 框架类型），保证未来安卓端可直接复用
- **禁止在 Core 中做任何 UI 假设**
    - 不得引用 `Dispatcher`、`Application.Current` 等 WPF 专属对象
    - 不得假设同步/异步执行环境（如直接 `Thread.Sleep` 阻塞调用方）
- **新增功能前必须先确认落点**
    - AI 在实现新功能时，先判断该功能属于 Core 还是 Wpf 层，并在代码注释或提交说明中简要说明理由
    - 若功能同时涉及两层（如"码表提示"这种既要查询码表又要在 UI 弹出候选框的功能），必须明确拆分接口边界（Core 提供查询结果，Wpf 只负责展示）

---

## 3. IME 与按键处理的特殊约束（高风险区域）

- **绝不允许简化处理 IME 逻辑**
    - 不得用"假设每次按键都直接上屏"的简单逻辑替代真实的 IME 组合过程模拟
    - 必须区分：IME 组合过程中的退格（未上屏，不计入正式错误统计但可用于键速分析）与已上屏字符被删除的退格（计入统计）
- **涉及输入法的代码必须附带说明**
    - 修改 `WM_KEYDOWN` / `WM_IME_CHAR` / `WM_CHAR` 相关代码时，必须在提交说明或代码注释中写清楚："这段代码处理的是按键阶段还是上屏阶段"
    - 不允许在没有实际测试（至少是逻辑推演）的情况下声称"IME 处理已完善"
- **禁止想当然地假设输入法行为**
    - 不同输入法（拼音、五笔、双拼等）的 IME 消息序列可能有差异，AI 在没有把握时应明确指出"此实现基于 XX 假设，建议用户用实际输入法测试验证"，而不是断言绝对正确

---

## 4. 数据与统计的准确性约束

- **统计指标定义必须与项目文档一致**
    - 键速、码长、字速、退格次数、错误率等指标，计算公式一旦在文档或此前对话中确定，AI 不得擅自更改口径
    - 如发现现有定义有歧义或问题，必须先提出疑问，而不是自行选一个"看起来合理"的公式
- **数据库 Schema 变更必须谨慎**
    - 涉及 SQLite 表结构变更时，必须提供迁移方案（不能默认用户是全新数据库，要考虑已有打字记录不能丢失）
    - 禁止在没有明确指示的情况下删除或重命名已有字段
- **禁止编造"看起来正确"的统计口径**
    - 例如"错误率"到底是"按键错误率"还是"字符错误率"还是"净准确率"，如未明确定义，AI 应先询问而非自行决定

---

## 5. 依赖与工具链约束

- **禁止随意引入新的 NuGet 包**
    - 引入任何第三方依赖前，必须说明引入理由、包名、大致用途
    - 优先使用 .NET 内置能力（如 System.Text.Json、Microsoft.Data.Sqlite），避免不必要的重量级依赖
- **打包发布相关代码改动需格外谨慎**
    - 涉及 Velopack、GitHub Actions workflow 的修改，必须完整给出改动后的文件内容，不能只给 diff 片段导致 YAML 缩进错误
    - 版本号、tag 规则的变更需要用户确认后才能应用到实际 workflow 中

---

## 6. 交互与沟通规范

- **AI 必须承认不确定性**
    - 遇到不确定的 Windows API 行为、WPF 渲染细节、IME 具体消息顺序等问题，应明确说"这一点我不确定，建议验证"，而不是给出自信但可能错误的答案
- **重大变更前先确认**
    - 涉及架构调整、接口签名变更、数据库 Schema 变更等"牵一发动全身"的修改，先用简短文字说明变更内容和影响范围，等用户确认后再动手
- **保持任务粒度可追踪**
    - 严格按照项目任务规划文档中的阶段推进，不跳跃开发未规划的功能
    - 完成一个任务节点后，简要说明"完成了什么、验证了什么、还有什么已知问题/未处理项"

---

## 7. 代码风格约束

- **命名与结构**
    - 遵循标准 C# 命名规范（PascalCase 类型/方法，camelCase 局部变量，接口以 `I` 开头）
    - Core 项目的公共类型需要有 XML 文档注释，说明用途与线程安全性假设
- **可测试性**
    - `TypingCore` 中的核心逻辑（比对状态机、统计计算）应尽量设计为纯函数或易于单元测试的形式，避免难以 Mock 的静态状态
    - AI 在实现核心算法时，若用户未明确要求写测试，也应主动询问是否需要补充单元测试

---

## 8. Git提交规范

### 格式

```
<emoji> <type>(<scope>): <subject>

<body>

<footer>
```

- **emoji**：视觉分类标识，必须使用
- **type**：`feat` / `fix` / `refactor` / `docs` / `test` / `chore` / `style` / `perf`
- **scope**：可选，如 `(opds)`、`(spider)`、`(api)`、`(web)`
- **subject**：中文标题，概括变更内容，首字无需空格
- **body**：英文或中英文混排，每行为一个 `- ` 开头的条目，描述具体变更
- **footer**：可选的 `Refs:` 或 `BREAKING CHANGE:`

### Emoji 对照表

| Type | Emoji | 含义 |
|---|---|---|
| `feat` | ✨ | 新功能 |
| `fix` | 🐛 | Bug 修复 |
| `refactor` | ♻️ | 代码重构 |
| `docs` | 📚 | 文档变更 |
| `test` | 🧪 | 测试相关 |
| `chore` | 🔧 | 工程化/依赖/配置 |
| `style` | 🎨 | 代码格式/样式 |
| `perf` | ⚡ | 性能优化 |
| `wip` | 🚧 | 进行中（仅临时使用，合并前必须 squash） |

### 示例

```
✨ feat(opds): 实现 OPDS 基础层——可见性控制与 EPUB 制品生命周期

- DB: add opds_visible, content_updated_at, epub_compiled_at columns
- Repository: add OPDS CRUD methods
- OpdsCompilationService: new cron-based scheduler

Refs: ROADMAP OPDS 书源服务构建与分发
```

```
🐛 fix(api): 修复定时更新策略变更后调度器未正确重载的并发问题
```

```
📚 docs: 添加 OPDS 书源服务任务到路线图
```

### 约定

- 多条变更在同一提交中时，`subject` 概括主要变更，`body` 逐条列举
- 每行 body 以 `- ` 开头，长度不超过 72 字符（英文）或适当截断
- **禁止**仅重复文件列表而无语义描述的提交
- **禁止**在提交消息中包含内部指令或占位符（如 "TODO"、"TBD"）

---

## 9. 违反本规范时的处理方式

若发现某次生成的代码违反了以上任意一条，应：

1. 立即在下一次回复中主动指出问题所在，不隐瞒、不回避
2. 提供修正方案
3. 不得为了"看起来完成了任务"而掩盖已知的实现缺陷

---

## 10. 测试规范

- **测试文件放置**
    - 测试项目为 `TypingCore.Tests`，目录结构必须镜像 `TypingCore` 的目录结构
    - 例如 `TypingCore/Engine/TypingSession.cs` 的测试放在 `TypingCore.Tests/Engine/TypingSessionTests.cs`
    - 测试类命名：`{被测类名}Tests`，如 `TypingSessionTests`、`ArticleImportServiceTests`
- **测试命名与组织**
    - 测试方法命名：`{方法名}_{场景描述}_{预期结果}`，使用下划线分隔
    - 例如 `ProcessInput_advances_session_when_commit_text_matches_target`
    - 每个测试方法只验证一个行为，避免在一个测试中混合多个断言场景
- **测试运行**
    - 运行全部测试：`dotnet test`
    - 运行指定项目：`dotnet test TypingCore.Tests`
    - 运行指定类：`dotnet test --filter "FullyQualifiedName~TypingSessionTests"`
- **测试约束**
    - 单元测试不得访问真实文件系统（除临时目录外）、不得发起网络请求
    - SQLite 测试使用内存数据库或临时文件，测试结束后清理
    - 测试中的时间相关断言使用固定的 `DateTimeOffset` 值，不得依赖 `DateTimeOffset.Now`

---

## 11. 文档更新规则

- **AI 不得自动修改的文件**
    - `ROADMAP.md`：仅用户明确要求时才能修改，AI 不得擅自标记任务完成或添加新阶段
    - `AGENTS.md`：AI 不得擅自修改自身的行为约束，如需调整应提出建议并等待用户确认
- **可自动更新的文档**
    - `README.md`：当项目结构、依赖、构建命令等发生变化时，AI 应主动建议更新
    - 各目录下的 `README.md`：当目录内模块职责发生变化时可更新
- **代码注释要求**
    - `TypingCore` 的公共类型（`public class/record/interface/enum`）必须有 XML 文档注释
    - 注释必须包含 `<summary>` 说明用途，`<remarks>` 说明线程安全性假设
    - 内部类型（`internal`）建议有注释但不强制

---

## 12. 常见任务的默认行为

- **实现新功能**
    - 先判断功能属于 Core 还是 Wpf 层
    - 先定义接口（放在 `Abstractions/`），再实现
    - 实现后主动询问是否需要补充单元测试
    - 遵循 ROADMAP 阶段顺序，不跳跃开发
- **修复 Bug**
    - 先定位问题所在层（Core 还是 Wpf）
    - 先编写能复现问题的测试（如果测试项目中还没有相关测试）
    - 修复后确认测试通过
- **重构代码**
    - 先说明重构目标和影响范围
    - 确保所有现有测试在重构后仍然通过
    - 不得在重构过程中引入新功能
- **添加新依赖**
    - 说明引入理由、包名、版本、用途
    - 优先使用 .NET 内置能力
    - 禁止引入未经验证的预发布版本包

---

## 13. WPF UI 编写规范

本节约束 `TypingCore.Wpf` 项目的视觉样式与控件编写，确保新增页面与现有"新中式纸韵"风格统一，避免回到早期界面的廉价感。

### 13.1 设计语言

- **整体风格**：新中式纸韵——温暖纸白底色 + 深棕主调 + 留白美学
- **设计参考**：Notion 的克制感 + 中式文化调性，不追求科技感或未来感
- **禁止风格**：大面积高饱和色拼接、毛玻璃特效、渐变按钮、Emoji 作为图标

### 13.2 色彩体系（必须使用以下色值，不得自行编造）

| 角色 | 色值 | 用途 |
|------|------|------|
| 纸白背景 | `#FAF8F5` | 主内容区底色 |
| 侧边栏 | `#F3EFEB` | 侧边栏背景 |
| 卡片白 | `#FFFFFF` | 卡片、输入框、下拉框背景 |
| 主强调 | `#8B5E3C` | 按钮、选中态、装饰条、聚焦边框 |
| 主强调 hover | `#7A5234` | 主按钮 hover 态 |
| 次强调 | `#C4A882` | 辅助装饰 |
| 标签胶囊底 | `#F0E6D6` | 标签胶囊背景 |
| 正文 | `#2D2926` | 主要文字 |
| 次级文字 | `#7A7067` | 说明文字、时间戳 |
| 边框 | `#E8E2DA` | 输入框、下拉框默认边框 |
| 错误 | `#C75146` | 错误状态 |
| 成功 | `#5B8C5A` | 正反馈 |

- **禁止在 XAML 中直接使用上述以外的硬编码色值**，如需新色值必须先在本节登记
- **禁止使用 WPF 默认色**（如 `White`、`Black`、`Transparent` 之外的命名色）
- 所有文字与背景组合必须满足 WCAG AA 4.5:1 对比度

### 13.3 排版规范

| 层级 | 字号 | 字重 | 用途 |
|------|------|------|------|
| 页面标题 | 28px | Bold (700) | 主窗口顶部标题栏 |
| 区域标题 | 18px | SemiBold (600) | 面板标题 |
| 卡片标题 | 15px | SemiBold (600) | 子面板标题 |
| 正文 | 14px | Regular (400) | 说明文字、按钮文字 |
| 辅助 | 12px | Regular (400) | 时间戳、标签、底部说明 |

- 行高统一 1.5，说明文字行高 1.6
- 禁止使用 12px 以下的字号

### 13.4 控件样式规范

#### 按钮

- **主按钮**：深棕底 `#8B5E3C` + 白字，圆角 10px，hover `#7A5234`
- **次按钮**：透明底 + `#8B5E3C` 描边 1px + `#8B5E3C` 文字，hover 浅棕底 `#F0E6D6`
- **高度** 40px，**左右内边距至少 28px**（禁止小于 24px，否则文字拥挤）
- **禁止**使用 WPF 原生 Button 默认样式，必须自定义 `ControlTemplate`
- **禁止**使用渐变背景、阴影按钮
- 禁用态用 `Opacity=0.5`，不得改变颜色

#### 输入框（TextBox）

- 白色背景 + `#E8E2DA` 1px 边框，圆角 10px
- 聚焦时边框变 `#8B5E3C`
- 高度 40px（搜索框）或 38px（普通输入）
- **禁止**使用 WPF 原生 TextBox 默认样式，必须自定义 `ControlTemplate`
- 搜索框左侧内嵌搜索图标（SVG Path），不得用文字"搜索"代替

#### 下拉框（ComboBox）

- **必须自定义 `ControlTemplate`**，禁止使用 Windows 原生下拉框样式
- 白色背景 + `#E8E2DA` 1px 边框，圆角 10px
- 右侧自定义下拉箭头（SVG Path，深棕色 `#8B5E3C`，1.5px 描边）
- hover 和展开时边框变 `#8B5E3C`
- 高度 40px

#### 卡片

- 白色背景 `#FFFFFF`
- 微投影：`DropShadowEffect` BlurRadius=12-16，ShadowDepth=2，Opacity=0.06
- 圆角 14-16px
- 内边距 20-24px
- **禁止**无投影的纯色卡片（这是早期廉价感的主要来源）
- 列表项卡片左侧可加 4px 宽深棕装饰条

#### 标签胶囊

- 背景 `#F0E6D6` + 文字 `#8B5E3C`
- 圆角 999px（全圆角）
- 内边距 10px 4px
- 字号 11-12px，SemiBold

### 13.5 图标规范

- **必须使用内嵌 SVG Path**（`<Path Data="M..."/>`），零依赖
- **禁止**使用 Emoji 作为功能图标
- **禁止**引入图标 NuGet 包（除非用户明确同意）
- 统一 1.5px 描边宽度，`StrokeEndLineCap="Round"` `StrokeStartLineCap="Round"`
- 导航项图标 20x20，搜索框图标 18x18，下拉箭头 16x16
- 图标 `Geometry` 资源定义在所在 XAML 的 `Resources` 中，用 `x:Key` 命名

### 13.6 布局规范

- 侧边栏宽度 280px，背景 `#F3EFEB`
- 主内容区外边距 28px
- 面板间距 16-18px
- 卡片间距 14px（列表项）
- 导航项高度 48px，间距 8px
- **禁止**使用 WPF 原生控件默认间距，所有间距必须显式指定

### 13.7 新增页面的强制要求

新增 WPF 页面（UserControl）时，必须：

1. 在 `Views/` 目录下创建 `.xaml` + `.xaml.cs`
2. 对应 ViewModel 放在 `ViewModels/`，继承 `PageViewModel`
3. 在 `MainWindow.xaml` 的 `Window.Resources` 中注册 `DataTemplate`
4. 在 `MainViewModel` 中添加导航命令和选中状态
5. 复用本节定义的色彩和控件样式，不得新建一套独立样式
6. 如需新色值或新控件样式，先在本节登记规范，再使用

### 13.8 样式复用

- 通用按钮、输入框、下拉框样式定义在各 View 的 `UserControl.Resources` 中
- 当同一样式被 3 个以上 View 使用时，应提取到 `App.xaml` 的 `Application.Resources` 中作为全局样式
- **禁止**在多个文件中复制粘贴色值而不使用样式资源
- 样式命名规范：`{控件类型}{用途}Style`，如 `PrimaryButtonStyle`、`SearchTextBoxStyle`
