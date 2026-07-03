# 阶段一骨架 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 创建可编译的 TypingPractice 解决方案骨架，并定义 TypingCore 的跨平台接口边界。

**Architecture:** 使用 .NET 解决方案承载三个项目。TypingCore 仅暴露平台无关契约；TypingCore.Wpf 负责 Windows 前端入口；TypingCore.Tests 用最小契约测试锁定 Core 的引用方式与接口表面。

**Tech Stack:** C# 12, .NET 8, WPF, xUnit

---

### Task 1: 生成解决方案与项目

**Files:**
- Create: TypingPractice.sln
- Create: TypingCore/TypingCore.csproj
- Create: TypingCore.Wpf/TypingCore.Wpf.csproj
- Create: TypingCore.Tests/TypingCore.Tests.csproj

- [ ] **Step 1: 生成解决方案文件**

Run: `dotnet new sln -n TypingPractice`
Expected: 创建 TypingPractice.sln

- [ ] **Step 2: 生成 Core 类库**

Run: `dotnet new classlib -n TypingCore -f net8.0`
Expected: 创建 TypingCore 项目

- [ ] **Step 3: 生成 WPF 项目**

Run: `dotnet new wpf -n TypingCore.Wpf -f net8.0-windows`
Expected: 创建 TypingCore.Wpf 项目

- [ ] **Step 4: 生成测试项目**

Run: `dotnet new xunit -n TypingCore.Tests -f net8.0`
Expected: 创建 TypingCore.Tests 项目

- [ ] **Step 5: 添加项目到解决方案并建立引用**

Run: `dotnet sln TypingPractice.sln add TypingCore/TypingCore.csproj TypingCore.Wpf/TypingCore.Wpf.csproj TypingCore.Tests/TypingCore.Tests.csproj`
Run: `dotnet add TypingCore.Wpf/TypingCore.Wpf.csproj reference TypingCore/TypingCore.csproj`
Run: `dotnet add TypingCore.Tests/TypingCore.Tests.csproj reference TypingCore/TypingCore.csproj`
Expected: 三个项目均在解决方案内，Wpf/Tests 均引用 Core

### Task 2: 先写失败的接口契约测试

**Files:**
- Modify: TypingCore.Tests/UnitTest1.cs

- [ ] **Step 1: 写失败测试，声明阶段一需要的接口表面**

测试应验证：

- Core 暴露 IKeyInputEvent、ITypingSession、IStatisticsProvider、ICodeTableProvider、IArticleRepository、ISessionRepository。
- Core 暴露 KeyInputKey 与 TypingSessionState。
- 测试项目可以引用这些类型并用本地 stub 实现它们。

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test TypingCore.Tests/TypingCore.Tests.csproj`
Expected: 由于接口与类型尚未定义而失败

### Task 3: 实现 Core 目录骨架与接口契约

**Files:**
- Create: TypingCore/Abstractions/KeyInputKey.cs
- Create: TypingCore/Abstractions/TypingSessionState.cs
- Create: TypingCore/Abstractions/IKeyInputEvent.cs
- Create: TypingCore/Abstractions/ITypingSession.cs
- Create: TypingCore/Abstractions/ITypingSessionSnapshot.cs
- Create: TypingCore/Abstractions/IStatisticsProvider.cs
- Create: TypingCore/Abstractions/IStatisticsSnapshot.cs
- Create: TypingCore/Abstractions/ICodeTableProvider.cs
- Create: TypingCore/Abstractions/ICodeLookupResult.cs
- Create: TypingCore/Abstractions/IArticleRepository.cs
- Create: TypingCore/Abstractions/ISessionRepository.cs
- Create: TypingCore/Abstractions/IArticleRecord.cs
- Create: TypingCore/Abstractions/ISessionRecord.cs
- Create: TypingCore/Models/.gitkeep
- Create: TypingCore/Parsing/.gitkeep
- Create: TypingCore/Engine/.gitkeep
- Create: TypingCore/Persistence/.gitkeep

- [ ] **Step 1: 实现最小契约类型与接口**

要求：

- 所有公共类型位于 TypingCore.Abstractions 命名空间。
- 公共类型带 XML 文档注释。
- 不引用任何 WPF 或 Windows 专属类型。

- [ ] **Step 2: 统一项目级配置**

要求：

- TypingCore 使用 net8.0。
- TypingCore.Wpf 使用 net8.0-windows 且启用 UseWPF。
- 三个项目统一开启 Nullable 与 ImplicitUsings。

### Task 4: 让测试转绿并做窄范围验证

**Files:**
- Modify: TypingCore.Tests/UnitTest1.cs
- Modify: ROADMAP.md

- [ ] **Step 1: 运行测试确认通过**

Run: `dotnet test TypingCore.Tests/TypingCore.Tests.csproj`
Expected: 测试通过

- [ ] **Step 2: 运行解决方案构建**

Run: `dotnet build TypingPractice.sln`
Expected: 构建成功

- [ ] **Step 3: 更新路线图勾选状态**

将阶段一及其子项标记为已完成。