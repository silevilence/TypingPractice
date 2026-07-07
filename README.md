# TypingPractice — 打字练习软件

一款面向中文输入法用户的桌面打字练习工具，支持逐字比对、实时统计、码表反查等功能。架构设计为 Core 库与 UI 层分离，为未来跨平台扩展预留接口。

## 功能特性

- **逐字比对引擎**：实时比对用户输入与原文，支持正确/错误/待输入三态标记
- **IME 输入法支持**：区分物理按键与 IME 上屏事件，正确处理拼音/五笔等输入法的组字过程
- **实时统计面板**：键速（KPM）、字速（CPM/WPM）、平均码长、退格次数、错误率等指标
- **文章导入**：支持 TXT 文件导入（自动识别 UTF-8/GBK 编码）和剪贴板文本导入
- **数据持久化**：SQLite 存储文章库与练习历史记录
- **码表反查**：导入码表文件（极点/小鹤格式），练习时自动显示当前字符的编码提示
- **个性化设置**：支持明暗主题、练习字体与字号、暂停/重来/布局快捷键
- **偏好持久化**：设置保存到本地 JSON，启动时自动恢复
- **自动更新**：启动时静默检查 GitHub Release 新版本，下载后下次启动自动应用

## 环境要求

- **操作系统**：Windows 10 及以上
- **运行时**：.NET 8.0 SDK
- **IDE**：Visual Studio 2022 或 VS Code（需安装 C# Dev Kit 扩展）

## 快速开始

```bash
# 1. 克隆仓库
git clone <repo-url>
cd TypingPractice

# 2. 还原依赖
dotnet restore

# 3. 运行测试
dotnet test

# 4. 启动 WPF 前端
dotnet run --project TypingCore.Wpf
```

## 构建与发布

```bash
# Debug 构建
dotnet build

# Release 构建
dotnet build -c Release

# 自包含发布（win-x64）
dotnet publish TypingCore.Wpf -c Release -r win-x64 --self-contained true

# 本地生成 Velopack 安装包与更新包
dotnet tool restore
dotnet publish TypingCore.Wpf -c Release -r win-x64 --self-contained true -o artifacts/publish
dotnet tool run vpk pack --packId TypingPractice --packVersion 0.1.0 --packDir artifacts/publish --mainExe TypingPractice.exe --channel win
```

版本 tag 采用 `v<major>.<minor>.<patch>`，例如 `v1.0.0`。推送匹配 tag 后，
GitHub Actions 会构建、测试、打包并创建 GitHub Release，上传 `Releases/`
目录中的安装包、更新包和 `RELEASES` 索引文件。

## 测试

```bash
# 运行全部测试
dotnet test

# 运行指定项目测试
dotnet test TypingCore.Tests

# 运行指定测试类
dotnet test --filter "FullyQualifiedName~TypingSessionTests"

# 采集覆盖率
dotnet test --collect:"XPlat Code Coverage"
```

测试项目使用 xUnit 框架，覆盖以下模块：
- `Engine/`：打字比对状态机（正常流、错误重打、IME 组字）
- `Parsing/`：文章导入与文本规范化（编码检测、换行处理、字符分类）
- `Persistence/`：SQLite 仓库 CRUD 与查询
- `Models/`：数据模型契约验证
- `Wpf/`：导航、输入消息、渲染布局、设置与快捷键行为

最近一次阶段十四检查结果：Core 行覆盖率 92.44%，分支覆盖率 80.67%。

## 项目结构

```
TypingPractice/
├── TypingPractice.sln              # 解决方案文件
├── Directory.Build.props           # 共享编译选项（nullable、implicit usings）
├── ROADMAP.md                      # 开发路线图
├── AGENTS.md                       # AI 协作规范
│
├── TypingCore/                     # 平台无关核心类库
│   ├── TypingCore.csproj
│   ├── Abstractions/               # 跨平台接口定义
│   │   ├── IKeyInputEvent.cs       # 标准化按键事件
│   │   ├── ITypingSession.cs       # 打字会话接口
│   │   ├── IStatisticsProvider.cs  # 统计指标输出
│   │   ├── ICodeTableProvider.cs   # 码表反查接口
│   │   ├── ICodeTableRepository.cs # 码表持久化
│   │   ├── IArticleRepository.cs   # 文章持久化
│   │   ├── ISessionRepository.cs   # 会话持久化
│   │   ├── IUserPreferencesRepository.cs # 用户偏好持久化
│   │   ├── IArticleImportService.cs
│   │   └── ...                     # 其他接口与枚举
│   ├── Engine/                     # 打字比对与统计引擎
│   │   ├── TypingSession.cs        # 核心状态机实现
│   │   └── CodeTableProvider.cs    # 码表查询与内存索引
│   ├── Models/                     # 数据模型（record 类型，不可变）
│   │   ├── Article.cs / ArticleChar.cs / ArticleTextLayout.cs
│   │   ├── SessionStatistics.cs / TypingSessionSnapshot.cs
│   │   ├── CodeTable.cs / CodeTableEntry.cs / CodeLookupResult.cs
│   │   ├── UserPreferences.cs / UserTheme.cs
│   │   └── ...
│   ├── Parsing/                    # 文章与码表解析
│   │   ├── ArticleImportService.cs
│   │   ├── ArticleTextLayoutBuilder.cs
│   │   ├── CodeTableParser.cs      # 码表文件解析
│   │   └── TextFileDecoder.cs      # 编码自动检测
│   └── Persistence/                # 数据访问
│       ├── SqliteDatabase.cs
│       ├── SqliteArticleRepository.cs
│       ├── SqliteSessionRepository.cs
│       ├── FileCodeTableRepository.cs    # 码表文件存储
│       └── JsonUserPreferencesRepository.cs # 偏好 JSON 存储
│
├── TypingCore.Wpf/                 # WPF 前端
│   ├── TypingCore.Wpf.csproj
│   ├── App.xaml / App.xaml.cs      # 入口，Velopack 初始化
│   ├── MainWindow.xaml / .cs       # 主窗口，导航框架
│   ├── Assets/                     # 应用图标
│   ├── Services/                   # 平台服务
│   │   ├── VelopackUpdateService.cs
│   │   ├── WindowMessageInputTranslator.cs
│   │   ├── ApplicationThemeManager.cs
│   │   ├── ClipboardService.cs
│   │   └── ...
│   ├── ViewModels/                 # MVVM 视图模型
│   │   ├── MainViewModel.cs
│   │   ├── TypingPracticeViewModel.cs
│   │   ├── ArticleLibraryViewModel.cs
│   │   ├── SettingsViewModel.cs
│   │   └── ...
│   └── Views/                      # 页面与自定义控件
│       ├── TypingPracticeView.xaml   # 打字练习页
│       ├── ArticleLibraryView.xaml   # 文章库页
│       ├── SettingsView.xaml         # 设置页
│       ├── HistoryView.xaml          # 历史记录页
│       ├── PracticeResultView.xaml   # 练习结果页
│       ├── CodeTableManagerView.xaml # 码表管理页
│       ├── InterleavedTypingRenderControl.cs  # 布局 A 渲染
│       └── FollowingTypingTextBlock.cs        # 布局 B 渲染
│
└── TypingCore.Tests/               # 单元测试
    ├── Engine/                     # 比对引擎与码表测试
    ├── Models/                     # 数据模型测试
    ├── Parsing/                    # 文章与码表解析测试
    ├── Persistence/                # 数据访问测试
    └── Wpf/                        # 前端行为测试
        ├── PhaseSevenViewModelTests.cs
        ├── PhaseEightTypingInputTests.cs
        ├── PhaseNineInterleavedRenderTests.cs
        ├── PhaseTenFollowingRenderTests.cs
        ├── PhaseElevenStatisticsTests.cs
        ├── PhaseTwelveCodeTableTests.cs
        └── PhaseThirteenSettingsTests.cs
```

## 技术栈

| 组件 | 技术 | 版本 |
|------|------|------|
| 语言 | C# | 12 |
| 运行时 | .NET | 8.0 |
| 前端框架 | WPF | .NET 8.0-windows |
| 数据库 | SQLite | via Microsoft.Data.Sqlite 8.0.0 |
| 测试框架 | xUnit | 2.5.3 |
| 测试工具 | Microsoft.NET.Test.Sdk | 17.8.0 |
| 覆盖率 | coverlet.collector | 6.0.0 |
| 打包发布 | Velopack | 1.2.0 |
| CI/CD | GitHub Actions | tag 触发发布 |

## 开发进度

**已完成**（阶段一 ~ 十四）：
- 项目初始化与架构搭建
- 数据模型与内部格式设计
- SQLite 数据持久化层
- 文章导入与解析模块
- 打字比对核心引擎
- 统计指标计算引擎
- WPF 前端（基础窗口、键盘事件、双布局渲染、统计面板、历史趋势图表）
- 码表导入、编码提示与码表管理
- 设置与个性化功能（主题、字体、快捷键、偏好持久化）
- 整体测试、边界处理与长文章输入优化
- 阶段十四手动 IME 测试清单（见 [`docs/testing/phase14-manual-ime-checklist.md`](docs/testing/phase14-manual-ime-checklist.md)）

**开发中**（阶段十五）：
- Velopack 打包与 GitHub Actions 自动发布（基础设施已搭建，待端到端验证）

详见 [`ROADMAP.md`](ROADMAP.md)。版本变更记录见 [`changelog.md`](changelog.md)。
