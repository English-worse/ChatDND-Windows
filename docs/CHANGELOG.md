# 修改记录

## 2026-09-23 - 新功能 - 修改人：Codex

### 问题描述

仓库尚无 .NET 解决方案、项目结构或可执行测试入口，后续任务无法在统一目标框架下开发。

### 解决方案

使用用户本地 .NET 10 SDK 创建 ChatDND 解决方案、Core 类库、WinForms 应用和 xUnit 测试项目，并通过最小 smoke test 验证 Core 程序集可加载。保留现有设计和实施文档。

### 修改明细

| 文件 | 改动点 | 改动类型 | 说明 |
|---|---|---|---|
| `.gitignore` | 忽略规则 | 配置变更 | 忽略构建、IDE、测试和本地 SDD 产物 |
| `global.json` | SDK 固定策略 | 配置变更 | 要求 .NET SDK 10.0.100 并可滚动到最新功能带 |
| `Directory.Build.props` | 公共编译属性 | 配置变更 | 启用可空引用、隐式 using、警告即错误和最新语言版本 |
| `ChatDND.sln` | 解决方案 | 新功能 | 包含 Core、App 和 Core.Tests 项目 |
| `src/ChatDND.Core/ChatDND.Core.csproj` | Core 项目 | 新功能 | 目标为 `net10.0-windows`，引用 NAudio 2.2.1 |
| `src/ChatDND.Core/Marker.cs` | Core 标记类型 | 新功能 | 为 bootstrap smoke test 提供程序集锚点 |
| `src/ChatDND.App/ChatDND.App.csproj` | WinForms 项目 | 新功能 | 目标为 `net10.0-windows`，引用 Core |
| `src/ChatDND.App/Program.cs` | WinForms 入口 | 新功能 | 保留最小 bootstrap 入口，删除模板占位窗口 |
| `tests/ChatDND.Core.Tests/ChatDND.Core.Tests.csproj` | 测试项目 | 新功能 | 目标为 `net10.0-windows`，启用 WinForms 并引用 Core |
| `tests/ChatDND.Core.Tests/BootstrapTests.cs` | smoke test | 新功能 | 验证 Core 程序集名称 |
| `docs/downloads.md` | 下载审计 | 配置变更 | 记录已安装的 .NET SDK 10.0.401 |

### 验证方法

运行 `dotnet test ChatDND.sln`，确认解决方案还原、编译并执行一个通过的 smoke test。

### 预期效果和潜在风险

后续任务可以在统一的 .NET 10 Windows 项目结构上增量实现。SDK 安装在用户目录，不在项目内提交；不同机器首次构建前可能仍需安装兼容的 .NET 10 SDK。
