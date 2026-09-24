<p align="center">
  <img src="src/ChatDND.App/Assets/ChatDND-256.png" width="160" alt="ChatDND icon">
</p>

<h1 align="center">ChatDND</h1>

<p align="center">
  Windows 应用级免打扰工具。一键静音微信、QQ、钉钉等聊天软件的提示音，同时保留其他应用的声音。
</p>

<p align="center">
  <a href="https://github.com/English-worse/ChatDND-Windows/releases/latest">下载最新版</a>
  ·
  <a href="#快速使用">快速使用</a>
  ·
  <a href="#安全边界">安全边界</a>
  ·
  <a href="#从源码构建">从源码构建</a>
</p>

## 简介

Windows 自带的“勿扰模式”主要抑制系统通知，无法拦截微信、QQ、钉钉等桌面应用自行播放的提示音。

ChatDND 工作在 Windows Core Audio 音频会话层，按进程路径识别目标聊天软件，对匹配的音频会话执行静音和恢复。它不修改聊天软件内部设置，不读取聊天内容，也不访问网络。

## 功能

- 一键开启或关闭应用级免打扰。
- 按完整进程路径识别微信、QQ、钉钉等应用。
- 覆盖多个已打开实例，并持续处理后来创建的音频会话。
- 不静音未选中的应用。
- 只恢复由 ChatDND 修改过的会话，保留用户原始静音状态。
- 关闭窗口后继续在系统托盘运行。
- 首次启动自动识别常见聊天软件，默认勾选并显示中文名称。
- 支持可选管理员模式，先显示中文风险提示，再请求 UAC。
- 程序异常退出后，下次启动会尝试恢复遗留的静音状态。
- 不安装驱动，不注入目标进程，不读取联系人、聊天记录或账号数据。

## 下载

前往 [Releases](https://github.com/English-worse/ChatDND-Windows/releases/latest) 下载：

- `ChatDND-v0.1.0-win-x64.zip`
- 解压后运行 `ChatDND.exe`

发布包是 `win-x64` 自包含版本，不需要单独安装 .NET Runtime。

## 快速使用

1. 启动 `ChatDND.exe`。
2. 在“添加当前应用”中选择需要免打扰的聊天软件。
3. 点击“开启免打扰”。
4. 程序会进入系统托盘持续运行。
5. 从托盘菜单可以重新打开主界面、进入设置或退出程序。

## 安全边界

- 只操作 Windows 音频会话的 Mute 状态。
- 不修改微信、QQ、钉钉的安装目录、配置、注册表或账号状态。
- 不读取或修改联系人、通讯录、聊天记录、好友状态或群设置。
- 不调用聊天软件私有 API，不模拟点击应用界面。
- 不联网，不上传数据。
- 管理员模式默认关闭，必须由用户确认风险后主动开启。

## 当前限制

- 应用还未创建音频会话时，极短的第一声提示音可能漏过。
- 免打扰会静音选中应用的全部音频，包括消息提示音、语音和视频声音。
- 受保护、独占模式或跨会话音频可能无法由普通权限修改。
- 当前仓库未声明开源许可证，代码公开用于查看、下载和问题反馈；二次分发或修改前请先联系作者。

## 系统要求

- Windows 10 或 Windows 11 x64。
- 使用 Release 自包含包时不需要额外安装 .NET。

## 从源码构建

需要：

- .NET 10 SDK
- Windows Desktop Runtime
- Python 3 + Pillow，仅在重新生成图标时需要

构建和测试：

```powershell
dotnet test ChatDND.sln
dotnet build ChatDND.sln --no-restore
```

生成图标：

```powershell
python tools/generate_chatdnd_icon.py
```

发布：

```powershell
dotnet publish src/ChatDND.App/ChatDND.App.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:PublishTrimmed=false
```

## 项目结构

- `src/ChatDND.Core`：音频会话、匹配、恢复日志、后台扫描和权限逻辑。
- `src/ChatDND.App`：WinForms 中文界面、托盘、设置和发布资源。
- `tests/ChatDND.Core.Tests`：核心行为和回归测试。
- `tools`：图标生成和内存测量脚本。
- `docs`：设计、实施和验收文档。
