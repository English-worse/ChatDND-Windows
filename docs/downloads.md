# 下载与依赖记录

本文件用于记录本项目需要联网下载的 SDK、NuGet 包和工具，便于审计和回滚。

## 计划安装

| 项目 | 版本/来源 | 安装位置 | 用途 | 状态 |
|---|---|---|---|---|
| .NET SDK | 10.0.401 GA，来源 `https://builds.dotnet.microsoft.com/dotnet/Sdk/10.0.401/dotnet-sdk-10.0.401-win-x64.zip` | `%USERPROFILE%\\.dotnet` | 编译、测试、发布 WinForms 项目 | 已安装 |
| NAudio | 2.2.1，来源 NuGet 官方源 | NuGet 缓存和项目 `csproj` | Windows Core Audio 会话枚举与静音 | 已还原 |
| xUnit | 2.9.3，来源 NuGet 官方源 | NuGet 缓存和测试项目 | 单元测试 | 已还原 |
| xunit.runner.visualstudio | 3.1.4，来源 NuGet 官方源 | NuGet 缓存和测试项目 | 测试适配器 | 已还原 |
| Microsoft.NET.Test.Sdk | 17.14.1，来源 NuGet 官方源 | NuGet 缓存和测试项目 | 测试宿主 | 已还原 |
| coverlet.collector | 6.0.4，来源 NuGet 官方源 | NuGet 缓存和测试项目 | 测试覆盖率收集 | 已还原 |

## 说明

- 所有联网下载都通过官方源或 GitHub 官方仓库完成。
- 不下载、运行或引入未在 `csproj` 中声明的二进制文件。
- `.dotnet/`、`bin/`、`obj/` 和 NuGet 缓存不提交到 Git；对应版本记录在 `global.json`、`Directory.Build.props` 和 `csproj` 中。
