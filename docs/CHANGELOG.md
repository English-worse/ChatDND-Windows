# 修改记录

## 2026-09-23 - Bug修复 - 严重程度：中 - 修改人：Codex

### 问题描述

应用规则匹配器无法按裸文件名回退：`ProcessPathNormalizer.TryNormalize` 会把 `WeChat.exe` 解析成当前目录下的完整路径，导致完整路径不可用时的文件名匹配分支失效。另有异常信息不准确、大小写折叠比较不稳和空集合缺少防御的问题。

### 根因分析

路径规范化只调用 `Path.GetFullPath`，没有先验证输入是否为根路径；匹配器使用 `ToUpperInvariant` 后再做普通相等比较，未统一使用 Windows 路径的序数忽略大小写规则；对 `null` 规则集合和空路径集合没有防御。

### 解决方案

只规范化根路径，裸文件名返回失败并进入文件名回退；路径比较改用 `StringComparison.OrdinalIgnoreCase`；补充空路径、空规则集合和空可执行路径的测试与防御；修正无效路径异常消息。

### 修改明细

| 文件 | 改动点 | 改动类型 | 说明 |
|---|---|---|---|
| `src/ChatDND.Core/Matching/ProcessPathNormalizer.cs` | 路径规范化和异常 | Bug修复 | 拒绝非根路径，保留原始大小写，修正异常消息 |
| `src/ChatDND.Core/Matching/AppRuleMatcher.cs` | 规则匹配 | Bug修复 | 支持裸文件名回退，使用序数忽略大小写比较，增加空值防御 |
| `tests/ChatDND.Core.Tests/Matching/ProcessPathNormalizerTests.cs` | 路径测试 | 测试 | 增加裸文件名和异常消息覆盖，更新大小写预期 |
| `tests/ChatDND.Core.Tests/Matching/AppRuleMatcherTests.cs` | 匹配测试 | 测试 | 增加裸文件名、空路径、空规则和空路径集合覆盖 |

### 验证方法

运行 `dotnet test tests/ChatDND.Core.Tests/ChatDND.Core.Tests.csproj --filter FullyQualifiedName~Matching`，随后运行 `dotnet test ChatDND.sln`。

### 预期效果和潜在风险

完整路径不可读取时仍可按显式配置的文件名匹配规则，降低静默漏匹配风险。裸文件名可能匹配多个同名的显式规则，因此规则仍应优先使用完整路径。

## 2026-09-23 - 新功能 - 修改人：Codex

### 问题描述

应用规则、音频会话标识和进程路径需要统一的数据模型与匹配规则，否则后续的多开识别和音频会话筛选会重复实现且容易产生路径大小写差异。

### 解决方案

增加 Core 领域模型、进程路径规范化和应用规则匹配器。完整路径采用规范化大小写不敏感比较；只有完整路径不可读取时才回退到文件名，并且显式路径之外的共享辅助进程不会被自动匹配。

### 修改明细

| 文件 | 改动点 | 改动类型 | 说明 |
|---|---|---|---|
| `src/ChatDND.Core/Models/AppRule.cs` | 应用规则 | 新功能 | 保存应用标识、显示名、可执行文件路径和启用状态 |
| `src/ChatDND.Core/Models/SessionKey.cs` | 音频会话标识 | 新功能 | 组合会话标识、会话实例标识和进程 ID |
| `src/ChatDND.Core/Models/SessionPlaybackState.cs` | 会话状态 | 新功能 | 表示音频会话的活动、非活动和过期状态 |
| `src/ChatDND.Core/Models/AudioSessionSnapshot.cs` | 音频会话快照 | 新功能 | 传递会话路径、静音状态和播放状态 |
| `src/ChatDND.Core/Matching/ProcessPathNormalizer.cs` | 路径规范化 | 新功能 | 去除引号和空白、转换完整路径并处理无效路径 |
| `src/ChatDND.Core/Matching/AppRuleMatcher.cs` | 规则匹配 | 新功能 | 按完整路径匹配启用规则，避免仅凭文件名误匹配 |
| `tests/ChatDND.Core.Tests/Matching/ProcessPathNormalizerTests.cs` | 路径测试 | 新功能 | 覆盖正常路径、空路径和无效路径 |
| `tests/ChatDND.Core.Tests/Matching/AppRuleMatcherTests.cs` | 匹配测试 | 新功能 | 覆盖大小写、禁用规则、同名异路径和未声明辅助进程 |

### 验证方法

运行 `dotnet test ChatDND.sln`，确认新增匹配逻辑和既有 bootstrap 测试全部通过。

### 预期效果和潜在风险

后续任务可以复用统一的应用规则和会话标识。不同应用的辅助进程仍需用户显式添加路径，否则对应声音不会被静音。

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
