# 修改记录

## 2026-09-23 - Bug修复 - 严重程度：中 - 修改人：Codex

### 问题描述

Task 9 审查发现，当规则存在但全部被禁用时，自动开启流程会抛出未捕获异常并导致程序在托盘出现前退出；从托盘切换免打扰后，已经打开的主窗口状态不会同步。

### 根因分析

启动判断只检查 `Rules.Count`，没有检查是否存在启用规则；托盘操作没有通知已创建的主窗口刷新控件状态。

### 解决方案

启动时只对至少一个启用规则自动开启；主窗口增加状态刷新方法并保存更新抑制标记，托盘切换后刷新已存在主窗口；主窗口只在用户主动关闭时隐藏到托盘，系统关机不再被拦截。

### 修改明细

| 文件 | 改动点 | 改动类型 | 说明 |
|---|---|---|---|
| `src/ChatDND.App/AppController.cs` | 自动开启判断 | Bug修复 | 只检查启用规则，避免全部禁用时启动崩溃 |
| `src/ChatDND.App/MainForm.cs` | 状态同步和关闭行为 | Bug修复 | 提供 `RefreshState`，托盘切换后同步，系统关机允许关闭 |
| `src/ChatDND.App/TrayApplicationContext.cs` | 托盘动作 | Bug修复 | 操作成功后刷新主窗口状态 |

### 验证方法

运行 `dotnet test ChatDND.sln` 和 `dotnet build ChatDND.sln --no-restore`。

### 预期效果和潜在风险

全部规则禁用时程序仍能正常启动；托盘与主窗口状态保持一致。管理员模式交接仍由 Task 10 实现。

## 2026-09-23 - 新功能 - 修改人：Codex

### 问题描述

核心逻辑已经可用，但缺少可操作的中文界面、托盘生命周期、应用规则管理和首次运行引导。

### 解决方案

新增 `UiStrings`、`AppController`、`MainForm`、`SettingsForm` 和 `TrayApplicationContext`。程序启动后创建托盘上下文；没有规则时打开中文主界面，存在规则且设置允许时自动开启免打扰。主界面支持发现候选应用、手动添加 EXE、删除规则和开关免打扰。

### 修改明细

| 文件 | 改动点 | 改动类型 | 说明 |
|---|---|---|---|
| `src/ChatDND.App/UiStrings.cs` | 中文文案 | 新功能 | 统一界面、托盘和风险提示文字 |
| `src/ChatDND.App/AppController.cs` | 应用状态控制 | 新功能 | 管理扫描计时器、规则、启动恢复和免打扰状态 |
| `src/ChatDND.App/MainForm.cs` | 中文主界面 | 新功能 | 开关、规则列表、添加/删除应用和状态显示 |
| `src/ChatDND.App/SettingsForm.cs` | 候选应用选择 | 新功能 | 从发现的进程中选择应用 |
| `src/ChatDND.App/TrayApplicationContext.cs` | 托盘生命周期 | 新功能 | 托盘菜单、主界面显示和安全退出 |
| `src/ChatDND.App/Program.cs` | 程序入口 | 新功能 | 组装 Core 服务和 WinForms 上下文 |
| `tests/ChatDND.Core.Tests/App/UiStringsTests.cs` | UI 文案测试 | 测试 | 验证主要界面文字为中文 |

### 验证方法

运行 `dotnet test ChatDND.sln`，并短暂启动 `ChatDND.App.exe` 验证进程保持运行。

### 预期效果和潜在风险

程序已经具备可运行的中文托盘界面和首次配置入口。真实多开音频会话仍需 Task 11 的本机集成验证。

## 2026-09-23 - 新功能 - 修改人：Codex

### 问题描述

程序需要统一的用户本地数据目录和中文日志，避免把运行数据写到目标聊天应用目录，也避免日志文件无限增长。

### 解决方案

新增 `AppPaths` 和 `RollingFileLog`，将设置、恢复日志和运行日志放在 `%LOCALAPPDATA%\ChatDND`；日志按大小滚动并保留一个历史文件。

### 修改明细

| 文件 | 改动点 | 改动类型 | 说明 |
|---|---|---|---|
| `src/ChatDND.Core/Services/AppPaths.cs` | 本地路径 | 新功能 | 统一设置、恢复日志和日志位置 |
| `src/ChatDND.Core/Logging/RollingFileLog.cs` | 滚动日志 | 新功能 | 线程安全的滚动文件日志 |
| `tests/ChatDND.Core.Tests/Services/AppPathsTests.cs` | 路径测试 | 测试 | 验证路径位于本地应用数据目录 |
| `tests/ChatDND.Core.Tests/Logging/RollingFileLogTests.cs` | 日志测试 | 测试 | 覆盖中文错误写入和滚动 |

### 验证方法

运行 `dotnet test tests/ChatDND.Core.Tests/ChatDND.Core.Tests.csproj --filter "FullyQualifiedName~AppPathsTests|FullyQualifiedName~RollingFileLogTests"`，再运行 `dotnet test ChatDND.sln`。

### 预期效果和潜在风险

程序运行数据保持在本用户目录，不会写入聊天应用数据目录。日志滚动只保留一个历史文件，旧日志会被覆盖。

## 2026-09-23 - 新功能 - 修改人：Codex

### 问题描述

首次运行需要向用户展示可配置的候选应用，避免没有规则时静默进入无效状态。

### 解决方案

新增 `CandidateApp` 和 `ProcessDiscoveryService`，从当前音频会话中发现进程，按规范化完整路径去重，跳过当前程序自身和无法规范化的路径，按显示名排序输出。

### 修改明细

| 文件 | 改动点 | 改动类型 | 说明 |
|---|---|---|---|
| `src/ChatDND.Core/Models/CandidateApp.cs` | 候选应用模型 | 新功能 | 保存显示名和进程路径 |
| `src/ChatDND.Core/Services/ProcessDiscoveryService.cs` | 候选发现 | 新功能 | 去重、跳过当前进程并生成候选列表 |
| `tests/ChatDND.Core.Tests/Services/ProcessDiscoveryServiceTests.cs` | 发现测试 | 测试 | 覆盖重复路径和当前进程排除 |

### 验证方法

运行 `dotnet test tests/ChatDND.Core.Tests/ChatDND.Core.Tests.csproj --filter FullyQualifiedName~ProcessDiscoveryServiceTests`，再运行 `dotnet test ChatDND.sln`。

### 预期效果和潜在风险

首次运行界面可以展示候选应用，但仍需要用户确认或手动选择 EXE 路径。

## 2026-09-23 - Bug修复 - 严重程度：高 - 修改人：Codex

### 问题描述

Task 6 审查发现 WASAPI 适配器在会话读取失败时会终止整轮扫描，未释放 NAudio 会话包装，并在进程路径读取失败时回退为 `PID:<id>`，导致按文件名匹配规则失效。

### 根因分析

设备枚举和会话读取共用一个外层异常处理；会话与音量包装没有在迭代范围内使用 `using`；路径回退没有尝试获取进程名。

### 解决方案

按设备、按会话分层隔离异常并记录警告；会话读取和静音操作使用 `using` 释放包装；路径失败时回退为 `ProcessName.exe`，仍无法获取时才使用 `PID:<id>`；扩大路径缓冲区到 Windows 最大路径长度。

### 修改明细

| 文件 | 改动点 | 改动类型 | 说明 |
|---|---|---|---|
| `src/ChatDND.Core/Audio/NaudioCoreAudioSessionSource.cs` | WASAPI 适配器 | Bug修复 | 会话/设备异常隔离、包装释放、过期会话跳过、进程名回退 |
| `src/ChatDND.Core/Interop/ProcessPathResolver.cs` | 路径解析 | Bug修复 | 使用 32767 字符缓冲区减少长路径失败 |

### 验证方法

运行 Task 6 聚焦测试和 `dotnet test ChatDND.sln`。真实设备行为留待 Task 11。

### 预期效果和潜在风险

单个会话消失或异常不再中断其他会话的枚举；完整路径不可读时仍可按显式文件名规则匹配。仍需真实多开实例验证。

## 2026-09-23 - 新功能 - 修改人：Codex

### 问题描述

核心协调器需要通过真实 Windows Core Audio 会话控制微信、QQ、钉钉等多开进程，并安全处理进程路径读取和设备枚举失败。

### 解决方案

新增进程路径解析、Core Audio 会话源、NAudio/WASAPI 提供器和控制器。枚举所有活动播放端点，按会话标识生成键，静音操作重新定位会话；单个设备或会话异常只记录警告，不终止整个扫描。

### 修改明细

| 文件 | 改动点 | 改动类型 | 说明 |
|---|---|---|---|
| `src/ChatDND.Core/Logging/ILog.cs` | 日志接口 | 新功能 | 为音频层和后续配置层提供统一日志入口 |
| `src/ChatDND.Core/Interop/ProcessPathResolver.cs` | 进程路径解析 | 新功能 | 使用 `QueryFullProcessImageName` 读取进程可执行文件路径 |
| `src/ChatDND.Core/Audio/ICoreAudioSessionSource.cs` | 音频源接口 | 新功能 | 抽象真实 NAudio 源和测试源 |
| `src/ChatDND.Core/Audio/CoreAudioSessionData.cs` | 音频源数据 | 新功能 | 保存会话键、进程路径、静音状态和播放状态 |
| `src/ChatDND.Core/Audio/CoreAudioSessionMapper.cs` | 快照映射 | 新功能 | 将音频源数据映射为核心快照 |
| `src/ChatDND.Core/Audio/NaudioCoreAudioSessionSource.cs` | WASAPI 适配器 | 新功能 | 枚举活动播放端点、读取会话并设置静音 |
| `src/ChatDND.Core/Audio/WasapiAudioSessionProvider.cs` | 会话提供器 | 新功能 | 将 Core Audio 映射为协调器接口 |
| `src/ChatDND.Core/Audio/WasapiAudioSessionController.cs` | 会话控制器 | 新功能 | 通过会话键设置静音 |
| `tests/ChatDND.Core.Tests/Audio/*` | 适配器测试 | 测试 | 覆盖映射、提供器和控制器委托 |
| `tests/ChatDND.Core.Tests/Interop/ProcessPathResolverTests.cs` | 路径测试 | 测试 | 验证当前进程路径解析 |

### 验证方法

运行聚焦的 `CoreAudioSessionMapper`、`ProcessPathResolver` 和 `WasapiAudioSessionAdapter` 测试，再运行 `dotnet test ChatDND.sln`。

### 预期效果和潜在风险

核心流程已经接入真实音频会话枚举和静音接口。NAudio 2.2.1 的会话集合不实现 `IDisposable`，枚举器仍需释放；受保护会话或设备切换仍可能返回空路径或失败，需要真实设备验证。

## 2026-09-23 - 新功能 - 修改人：Codex

### 问题描述

程序异常退出后，之前被工具静音的会话需要在下一次启动时安全恢复，且恢复失败不能再丢失记录。

### 解决方案

新增 JSON 恢复日志和启动恢复服务。恢复时只处理仍存活、由工具静音且原本未静音的会话；恢复失败的记录保留在日志中，下一次启动继续尝试。

### 修改明细

| 文件 | 改动点 | 改动类型 | 说明 |
|---|---|---|---|
| `src/ChatDND.Core/Services/JsonRecoveryJournal.cs` | 恢复日志持久化 | 新功能 | 原子写入、损坏备份、清空和异常读写处理 |
| `src/ChatDND.Core/Services/RecoveryService.cs` | 启动恢复 | 新功能 | 只恢复工具修改过的会话，失败记录保留 |
| `tests/ChatDND.Core.Tests/Services/JsonRecoveryJournalTests.cs` | 日志测试 | 测试 | 覆盖往返保存、清空和损坏 JSON |
| `tests/ChatDND.Core.Tests/Services/RecoveryServiceTests.cs` | 恢复测试 | 测试 | 覆盖恢复、原始静音保护和失败保留 |

### 验证方法

运行 `dotnet test tests/ChatDND.Core.Tests/ChatDND.Core.Tests.csproj --filter FullyQualifiedName~Recovery`，再运行 `dotnet test ChatDND.sln`。

### 预期效果和潜在风险

异常退出后不再静默丢失恢复机会。真实音频设备是否允许解除静音仍由 Task 6 和 Task 11 验证。

## 2026-09-23 - Bug修复 - 严重程度：中 - 修改人：Codex

### 问题描述

规则被移除时，如果会话解除静音失败，协调器仍会删除恢复记录，导致该会话后续无法再被恢复；同时 `ApplyRules` 没有对重复 `SessionKey` 去重，与 `Disable` 的行为不一致。

### 根因分析

规则移除分支忽略了 `TrySetMute` 的返回值并立即删除字典记录；扫描会话时直接遍历原始列表，没有复用按会话键去重的辅助方法。现有测试也没有覆盖这两个分支。

### 解决方案

规则移除恢复失败时保留记录并继续写入恢复日志；`ApplyRules` 统一通过 `GetSessionsByKey` 去重；增加规则移除成功、规则移除失败和重复会话键测试；修正测试替身在失败时仍写状态的问题。

### 修改明细

| 文件 | 改动点 | 改动类型 | 说明 |
|---|---|---|---|
| `src/ChatDND.Core/Services/DndCoordinator.cs` | 规则移除和去重 | Bug修复 | 失败时保留恢复记录，扫描会话按 SessionKey 统一去重 |
| `tests/ChatDND.Core.Tests/Services/DndCoordinatorTests.cs` | 回归测试 | 测试 | 覆盖规则移除、恢复失败和重复键 |
| `tests/ChatDND.Core.Tests/Services/Fakes/FakeAudioSessionController.cs` | 测试替身 | 测试 | 失败时不写入状态，支持失败注入 |

### 验证方法

运行聚焦的 `DndCoordinatorTests`，再运行 `dotnet test ChatDND.sln`。

### 预期效果和潜在风险

规则变更和重复会话不会再丢失恢复记录。真实 WASAPI 的重复会话行为仍需在 Task 6 和 Task 11 中验证。

## 2026-09-23 - 新功能 - 修改人：Codex

### 问题描述

需要把音频会话枚举、静音控制和免打扰状态机解耦，确保多开实例、原始静音状态恢复和规则变更都能在单元测试中验证。

### 解决方案

新增音频会话提供器和控制器接口、恢复日志接口、免打扰状态结果模型和 `DndCoordinator`。协调器统一管理会话原状态、逐个实例静音、处理新会话和规则移除，并只在恢复失败时保留恢复记录。

### 修改明细

| 文件 | 改动点 | 改动类型 | 说明 |
|---|---|---|---|
| `src/ChatDND.Core/Audio/IAudioSessionProvider.cs` | 会话枚举接口 | 新功能 | 为真实 WASAPI 和测试假实现提供统一入口 |
| `src/ChatDND.Core/Audio/IAudioSessionController.cs` | 会话控制接口 | 新功能 | 返回真实静音操作是否成功 |
| `src/ChatDND.Core/Services/IRecoveryJournal.cs` | 恢复日志接口 | 新功能 | 支持保存、加载和清理恢复记录 |
| `src/ChatDND.Core/Services/DndCoordinator.cs` | 免打扰协调器 | 新功能 | 多实例静音、新会话处理、原状态恢复和规则变更同步 |
| `src/ChatDND.Core/Models/RecoveryRecord.cs` | 恢复记录模型 | 新功能 | 保存会话键、原静音状态和是否由工具修改 |
| `src/ChatDND.Core/Models/DndEnableResult.cs` | 启用结果 | 新功能 | 区分已启用和缺少规则 |
| `tests/ChatDND.Core.Tests/Services/DndCoordinatorTests.cs` | 协调器测试 | 测试 | 覆盖多实例、无规则、原状态恢复和新会话 |
| `tests/ChatDND.Core.Tests/Services/Fakes/*` | 测试替身 | 测试 | 提供可观测的会话提供器、控制器和恢复日志 |

### 验证方法

运行 `dotnet test tests/ChatDND.Core.Tests/ChatDND.Core.Tests.csproj --filter FullyQualifiedName~DndCoordinatorTests` 和 `dotnet test ChatDND.sln`。

### 预期效果和潜在风险

核心状态机可以在不依赖真实音频设备的情况下测试。真实设备行为仍需要 Task 6 的 WASAPI 适配器和 Task 11 的本机集成验证。

## 2026-09-23 - 新功能 - 修改人：Codex

### 问题描述

应用规则需要本地持久化，且损坏配置和异常扫描间隔不能导致程序启动失败。

### 解决方案

新增 `DndSettings` 和 `AppRuleStore`，使用 `System.Text.Json` 读写本地配置。加载时规范化扫描间隔到 100 至 5000 毫秒，遇到损坏 JSON 时保留备份并回退到默认配置。

### 修改明细

| 文件 | 改动点 | 改动类型 | 说明 |
|---|---|---|---|
| `src/ChatDND.Core/Models/DndSettings.cs` | 配置模型 | 新功能 | 保存自动开启、托盘、管理员启动、扫描间隔和应用规则 |
| `src/ChatDND.Core/Services/AppRuleStore.cs` | 配置存储 | 新功能 | 原子保存、损坏备份、默认值回退和扫描间隔约束 |
| `tests/ChatDND.Core.Tests/Services/AppRuleStoreTests.cs` | 存储测试 | 新功能 | 覆盖读写、损坏配置和异常扫描间隔 |

### 验证方法

运行 `dotnet test tests/ChatDND.Core.Tests/ChatDND.Core.Tests.csproj --filter FullyQualifiedName~AppRuleStoreTests` 和 `dotnet test ChatDND.sln`。

### 预期效果和潜在风险

后续 UI 可以稳定保存和恢复用户规则。配置目录仍需由上层使用用户本地目录，不能写入目标聊天应用的数据目录。

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
