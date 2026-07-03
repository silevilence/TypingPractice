# 阶段一：项目初始化与架构搭建设计

## 目标

完成可编译的项目骨架，建立 TypingCore 与 TypingCore.Wpf 的分层边界，并为后续文章解析、输入法事件处理、统计计算与持久化提供跨平台接口入口。

## 范围

本阶段包含以下内容：

- 创建解决方案与三个项目：TypingCore、TypingCore.Wpf、TypingCore.Tests。
- 建立项目引用关系，确保 Wpf 与 Tests 依赖 Core。
- 统一项目级编码配置：nullable、implicit usings。
- 在 TypingCore 中建立 Models、Parsing、Engine、Persistence、Abstractions 目录骨架。
- 定义跨平台核心接口：IKeyInputEvent、ITypingSession、IStatisticsProvider、ICodeTableProvider、IArticleRepository、ISessionRepository。
- 为接口提供最小契约类型，使 Core 可被编译与测试消费。

本阶段不包含以下内容：

- 阶段二的数据实体字段定稿。
- 输入法状态机实现。
- SQLite 持久化实现。
- WPF 页面、MVVM 或窗口导航实现。

## 设计选择

### 方案选择

采用“最小接口骨架”方案，而不是提前落入实体实现或占位实现类。

原因：

- 阶段一路线图的核心目标是搭建结构与边界，不是提前锁定业务模型。
- 过早引入 Article、TypingSession、SessionStatistics 等完整实体，会把阶段二的建模决策前置。
- 空实现类会引入当前无业务价值的生产代码，并增加后续清理成本。

### 分层约束

- TypingCore 只包含平台无关代码，不得引用 WPF、Win32 或 System.Windows.*。
- TypingCore.Wpf 仅作为 Windows 展示层与输入事件翻译层存在。
- TypingCore.Tests 用于验证 Core 边界的可编译性与消费方式。

## 结构设计

### 解决方案结构

- TypingPractice.sln
- TypingCore
- TypingCore.Wpf
- TypingCore.Tests

### Core 目录职责

- Models：后续承载文章、会话、统计等纯数据模型。
- Parsing：后续承载文章导入、文本清洗、分字与码表解析。
- Engine：后续承载比对状态机与统计计算逻辑。
- Persistence：后续承载 SQLite 仓储实现。
- Abstractions：当前阶段承载跨平台契约与边界类型。

## 接口边界

### 输入事件

IKeyInputEvent 表达统一后的按键事件，不承载任何 WPF 原始事件类型。该接口至少表达：键值、时间戳、是否来源于 IME、IME 上屏文本、是否为退格。

### 打字会话

ITypingSession 表达核心引擎的输入入口与状态出口。当前阶段仅定义行为边界，不定义具体状态机算法。

### 统计输出

IStatisticsProvider 作为统计快照出口，用于后续实时面板与结果页复用。

### 码表与仓储

ICodeTableProvider、IArticleRepository、ISessionRepository 只定义查询/保存边界，不引入具体持久化实现。

## 最小契约类型

为了避免阶段一接口只剩 object 或 string 拼凑，补充以下轻量类型：

- KeyInputKey：统一键值枚举。
- TypingSessionState：会话状态枚举。
- ITypingSessionSnapshot：当前会话快照接口。
- IStatisticsSnapshot：统计快照接口。
- ICodeLookupResult：码表查询结果接口。
- IArticleRecord 与 ISessionRecord：仓储记录接口。

这些类型只提供边界意义，不定义阶段二业务字段全集。

## 测试与验证

- 先在 TypingCore.Tests 中编写接口契约测试，证明 Core 的边界能被引用与实例化。
- 使用窄范围测试验证公共接口的可消费性。
- 使用解决方案级构建验证模板、项目引用与跨项目编译关系。

## 已知约束

- 当前工作区尚未初始化 git 仓库，因此本次只写入规格文档，不执行提交。
- WPF 模板与 .NET 8 SDK 已在本机确认可用，可直接进行脚手架生成。