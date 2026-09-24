# ChatDND Windows Acceptance Checklist

## 自动检查

- [x] `dotnet test ChatDND.sln -c Release` passes.
- [x] `dotnet build ChatDND.sln -c Release --no-restore` completes with 0 warnings and 0 errors.
- [x] Framework-dependent `win-x64` publish succeeds.
- [x] The published executable starts and remains running for a smoke-test interval.
- [x] `tools/Measure-ChatDND.ps1` records idle working set values.
- [ ] 24-hour long-run memory soak test.

## 手工验收

- [ ] First launch shows Chinese candidate applications.
- [ ] With no rules, enabling DND shows the Chinese no-rules message and does not enter a fake enabled state.
- [ ] Two instances of the same chat application are both muted.
- [ ] A third instance launched after DND is enabled is muted within 1 second.
- [ ] An unselected media player remains audible.
- [ ] Closing the main window hides it to the tray and keeps DND enabled.
- [ ] Tray exit restores sessions that were originally audible.
- [ ] A session that was already muted before DND remains muted after exit.
- [ ] Switching to a Bluetooth or virtual render endpoint still finds and mutes sessions.
- [ ] Force-closing ChatDND and reopening it restores the recovery journal.
- [ ] Running without administrator rights works for normal chat applications.
- [ ] Choosing admin mode shows the Chinese risk dialog before UAC.
- [ ] Approving UAC lets the elevated instance take over.
- [ ] Cancelling UAC leaves the ordinary instance running.
- [ ] The UI, tray menu, dialogs, and errors are Chinese; English technical terms have Chinese explanations.

## 结果记录

- 自动检查结果：Release 测试 44/44 通过，Release 构建 0 警告/0 错误，win-x64 framework-dependent 发布成功，发布程序启动存活，10 秒空闲工作集 49.37-49.42 MB，平均 49.395 MB。
- 手工检查环境：
- 内存测量结果：
- 遗留问题：
