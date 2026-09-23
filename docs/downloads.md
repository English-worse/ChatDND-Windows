# 下载与依赖记录

本文件用于记录本项目需要联网下载的 SDK、NuGet 包和工具，便于审计和回滚。

## 计划安装

| 项目 | 版本/来源 | 安装位置 | 用途 | 状态 |
|---|---|---|---|---|
| .NET SDK | 10.0 GA | `%USERPROFILE%\\.dotnet` | 编译、测试、发布 WinForms 项目 | 待安装 |
| NAudio | 2.2.1 | NuGet 缓存和项目 `csproj` | Windows Core Audio 会话枚举与静音 | 待还原 |
| xUnit | 模板生成版本 | NuGet 缓存和测试项目 | 单元测试 | 待还原 |
| Microsoft.NET.Test.Sdk | 模板生成版本 | NuGet 缓存和测试项目 | 测试宿主 | 待还原 |

## 说明

- 所有联网下载都通过官方源或 GitHub 官方仓库完成。
- 不下载、运行或引入未在 `csproj` 中声明的二进制文件。
- `.dotnet/`、`bin/`、`obj/` 和 NuGet 缓存不提交到 Git；对应版本记录在 `global.json`、`Directory.Build.props` 和 `csproj` 中。
