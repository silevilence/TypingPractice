# 项目开发路线图 (Roadmap)

## 📅 计划中

## 🚧 开发中

- [x] **阶段四：文章导入与解析模块**
    - [x] 实现 TXT 文件导入
        - [x] 编码自动识别（UTF-8/GBK 等）
        - [x] 文本清洗（去除多余空行、统一换行符）
    - [x] 实现剪贴板文本导入接口（预留给 Wpf 层调用）
    - [x] 实现文章分字与内部格式转换算法
        - [x] 处理中文字符、英文单词、数字、标点的分类标记
        - [x] 为逐字对齐渲染预先计算每个字符的显示宽度类型（全角/半角）
    - [x] 编写单元测试覆盖各类文本边界情况（纯中文、中英混排、含符号、含换行）

- [ ] **阶段五：打字比对核心引擎（最关键模块）**
    - [ ] 实现按键事件标准化处理
        - [ ] 定义 `IKeyInputEvent` 的具体使用流程（区分原始按键 vs IME 上屏事件）
        - [ ] 编写按键事件到内部状态的转换逻辑
    - [ ] 实现逐字比对状态机
        - [ ] 当前字指针管理（已完成/当前/未到）
        - [ ] 字符匹配判断（正确/错误标记）
        - [ ] 错误后允许退格重打的状态回退逻辑
    - [ ] 实现退格逐字区分逻辑
        - [ ] 区分"IME 组字内部退格"（不影响统计）与"已上屏字符退格"（计入退格统计）
    - [ ] 编写完整单元测试
        - [ ] 模拟正常打字流
        - [ ] 模拟错误重打流
        - [ ] 模拟中文输入法组字流（拼音输入多按键对应一字）

- [ ] **阶段六：统计指标计算引擎**
    - [ ] 实现实时统计计算
        - [ ] 键速（按键数/分钟）
        - [ ] 字速（上屏字数/分钟，CPM）
        - [ ] 平均码长（总按键数/上屏字数）
        - [ ] 退格次数与退格率
        - [ ] 错误率（错误字数/总字数）
    - [ ] 实现滑动窗口算法平滑瞬时速度波动
    - [ ] 实现会话结束后的汇总统计生成
    - [ ] 编写单元测试验证各指标计算准确性

- [ ] **阶段七：WPF 前端 —— 基础窗口与导航**
    - [ ] 搭建主窗口框架与页面导航结构（MVVM 模式，引入 CommunityToolkit.Mvvm）
    - [ ] 实现文章库页面
        - [ ] 文章列表展示
        - [ ] 导入文章按钮（文件选择/粘贴导入）
        - [ ] 文章搜索与标签筛选
    - [ ] 实现设置页面框架（主题、字体、快捷键，后续填充）

- [ ] **阶段八：WPF 前端 —— 键盘与 IME 事件捕获**
    - [ ] 实现 Win32 消息钩子（`HwndSource.AddHook`）捕获 `WM_KEYDOWN`
    - [ ] 实现 `WM_IME_CHAR` / `TextInput` 事件捕获上屏内容
    - [ ] 将 WPF 原始事件转换为 Core 库的 `IKeyInputEvent` 并喂入 `ITypingSession`
    - [ ] 处理特殊键（Backspace、Esc、方向键等的界面响应）
    - [ ] 针对常见输入法（微软拼音、搜狗、RIME）进行组字场景实测调试

- [ ] **阶段九：WPF 前端 —— 布局 A（逐字交错对齐渲染）**
    - [ ] 实现自定义渲染控件（继承 `FrameworkElement`，重写 `OnRender`）
    - [ ] 实现字符等宽单元格布局算法（全角/半角字符宽度归一化）
    - [ ] 实现原文行与输入行的逐字对齐绘制
    - [ ] 实现当前字符高亮、错误字符标红、已完成字符样式
    - [ ] 性能优化（避免每次按键全量重绘，采用局部 InvalidateVisual）

- [ ] **阶段十：WPF 前端 —— 布局 B（上下分栏跟随渲染）**
    - [ ] 实现原文区域的分段染色渲染（已过/当前/未到/错误 四态）
    - [ ] 实现输入区域自由输入展示
    - [ ] 实现随打字进度自动滚动/翻页逻辑
    - [ ] 布局切换功能（用户可在 A/B 布局间切换，共享同一比对引擎状态）

- [ ] **阶段十一：WPF 前端 —— 实时统计与结果展示**
    - [ ] 练习过程中的实时数据面板（键速、字速、码长、错误数）
    - [ ] 练习结束后的结果汇总页面
    - [ ] 历史记录页面（按文章查看历次练习记录列表）
    - [ ] 历史趋势图表（集成 LiveCharts2，展示速度/错误率随时间变化曲线）

- [ ] **阶段十二：码表导入与编码提示功能**
    - [ ] 实现码表文件解析器（支持常见码表格式，如极点/小鹤格式的文本码表）
    - [ ] 实现码表加载与内存索引构建（`ICodeTableProvider` 实现）
    - [ ] 实现打字过程中的编码提示逻辑
        - [ ] 根据当前及后续字符反查候选编码
        - [ ] 提示浮窗 UI（显示候选编码与说明）
    - [ ] 码表管理页面（导入、切换、删除码表）

- [ ] **阶段十三：设置与个性化功能完善**
    - [ ] 主题切换（明暗模式）
    - [ ] 字体与字号自定义
    - [ ] 快捷键自定义（如暂停/重来/切换布局）
    - [ ] 用户偏好持久化（写入本地配置文件或 SQLite）

- [ ] **阶段十四：整体测试与优化**
    - [ ] Core 库单元测试补全与覆盖率检查
    - [ ] 端到端手动测试（真实输入法环境下多轮测试）
    - [ ] 性能测试（长文章、高频按键下的渲染与统计性能）
    - [ ] 异常处理与边界情况修复（空文章、超长文章、码表格式错误等）

- [ ] **阶段十五：打包与发布（Velopack + GitHub Release + GitHub Actions）**
    - [ ] 本地打包基础配置
        - [ ] 配置 `dotnet publish` 自包含发布参数（`-r win-x64 --self-contained`）
        - [ ] 启用裁剪（`PublishTrimmed`）优化体积
        - [ ] 制作应用图标与版本信息（`AssemblyInfo` / csproj 中的版本号字段）
    - [ ] 集成 Velopack
        - [ ] 安装 `vpk` CLI 工具（`dotnet tool install -g vpk`）
        - [ ] 项目引入 `Velopack` NuGet 包，初始化 `VelopackApp.Build().Run()` 启动钩子
        - [ ] 本地验证打包命令（`vpk pack` 生成安装包与全量/增量更新包）
        - [ ] 验证自动更新流程（`UpdateManager` 检查更新、下载、应用重启）
    - [ ] 连接 Velopack 与 GitHub Release
        - [ ] 配置 `vpk` 的 GitHub 发布参数（仓库地址、Release 资产上传）
        - [ ] 生成 GitHub Personal Access Token（或使用 Actions 内置 `GITHUB_TOKEN`）并配置好所需权限范围
        - [ ] 本地手动执行一次 `vpk upload github` 验证发布链路可用
    - [ ] 版本号规范与 Tag 约定
        - [ ] 确定版本号规则（如语义化版本 `v1.0.0`）
        - [ ] 确定 Tag 命名格式与项目内版本号的对应关系
    - [ ] 编写 GitHub Actions 工作流
        - [ ] 创建 `.github/workflows/release.yml`
        - [ ] 配置触发条件：监听匹配版本号规则的 tag 推送（如 `v*.*.*`）
        - [ ] 配置构建步骤：checkout 代码、安装 .NET SDK、还原依赖
        - [ ] 配置发布步骤：`dotnet publish` → 安装/调用 `vpk` → 打包生成安装包与更新包
        - [ ] 配置自动创建 GitHub Release 并上传构建产物（安装包、更新包、`RELEASES` 索引文件）
        - [ ] Release 说明内容生成方式确定（后续再定具体模板，如自动生成 changelog 或手动填写）
    - [ ] 端到端验证
        - [ ] 推送测试 tag，验证 Actions 自动触发、构建、发布全流程
        - [ ] 验证已安装客户端能通过 Velopack 自动检测并升级到新发布的版本
    - [ ] 编写用户使用说明文档（下载安装、更新机制说明）

## ✅ 已完成

- [x] **阶段一：项目初始化与架构搭建**
    - [x] 使用 dotnet CLI 创建解决方案结构
        - [x] `dotnet new sln -n TypingPractice`
        - [x] `dotnet new classlib -n TypingCore -f net8.0`
        - [x] `dotnet new wpf -n TypingCore.Wpf -f net8.0`
        - [x] `dotnet new xunit -n TypingCore.Tests -f net8.0`
        - [x] 将三个项目 add 到 sln，配置 Wpf 与 Tests 项目引用 Core 项目
    - [x] 配置项目文件属性
        - [x] TypingCore 设置 `<TargetFramework>net8.0</TargetFramework>`（不引用任何 Windows 专属包）
        - [x] TypingCore.Wpf 设置 `<TargetFramework>net8.0-windows</TargetFramework>` 并启用 `UseWPF`
        - [x] 统一 nullable 引用类型、隐式 using 等编码规范
    - [x] 建立 Core 库内部目录结构
        - [x] `Models/`（数据模型）
        - [x] `Parsing/`（文章与码表解析）
        - [x] `Engine/`（打字比对与统计引擎）
        - [x] `Persistence/`（数据访问层）
        - [x] `Abstractions/`（跨平台接口定义，为 Android 预留）
    - [x] 设计并定义跨平台核心接口（提前预留多平台扩展点）
        - [x] `IKeyInputEvent`：标准化按键事件（Key、Timestamp、IsFromIME、ImeCommitText、IsBackspace 等）
        - [x] `ITypingSession`：打字会话接口（输入事件、当前状态、比对结果）
        - [x] `IStatisticsProvider`：统计指标输出接口
        - [x] `ICodeTableProvider`：码表反查接口
        - [x] `IArticleRepository` / `ISessionRepository`：数据持久化接口（面向接口编程，具体实现在 Persistence 层）

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

- [x] **阶段三：SQLite 数据持久化层（Core 库内）**
    - [x] 引入 `Microsoft.Data.Sqlite`（纯 .NET，无平台依赖）
    - [x] 设计数据库表结构
        - [x] `articles` 表
        - [x] `sessions` 表
        - [x] `keystrokes` 表（或按需汇总，避免海量明细影响性能）
        - [x] `codetables` 表
    - [x] 实现 `SqliteArticleRepository`（实现 `IArticleRepository`）
        - [x] 增删改查文章
        - [x] 按标题/标签搜索
    - [x] 实现 `SqliteSessionRepository`（实现 `ISessionRepository`）
        - [x] 保存单次练习记录与统计结果
        - [x] 按文章查询历史记录列表
        - [x] 按时间范围查询统计趋势数据
    - [x] 数据库初始化与迁移逻辑
        - [x] 首次启动自动建表
        - [x] 版本号管理，为后续字段升级预留迁移脚本机制
