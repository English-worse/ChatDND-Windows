# ChatDND Windows App-Level Do Not Disturb Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a low-memory Windows tray application that mutes all audio sessions belonging to selected chat applications, including multiple instances and newly created sessions, while restoring only the sessions it changed.

**Architecture:** Keep all Windows audio, process matching, persistence, recovery, and privilege logic in `ChatDND.Core`; keep WinForms and tray UI in `ChatDND.App`. Use NAudio to enumerate Core Audio render endpoints and audio sessions, poll every 500 ms while DND is enabled, and pass test fakes through narrow interfaces.

**Tech Stack:** C# 14, .NET 10 Windows, WinForms, NAudio 2.2.1, xUnit, System.Text.Json, Windows Core Audio/WASAPI, P/Invoke for `QueryFullProcessImageName`.

**Spec:** `docs/superpowers/specs/2026-09-23-windows-chat-dnd-design.md`

## Global Constraints

- Target Windows 10/11 x64.
- Use .NET 10 Windows. The current machine has .NET 10 runtimes but no .NET SDK; Task 1 installs a user-local SDK if `dotnet --list-sdks` is empty.
- Default to normal user privileges. Admin mode is opt-in and must show the Chinese risk dialog before UAC.
- Never modify contacts, chat data, account state, application settings, application installation files, or application registry keys.
- Never use UI automation against WeChat, QQ, DingTalk, or any target application.
- Never inject a DLL, install a driver, hook a process, or send network traffic.
- Enumerate all active render endpoints, not only the default endpoint.
- Scan only while DND is enabled; use a 500 ms interval by default and one serial scan loop.
- Match by normalized process path; use the executable filename only when the full path cannot be read.
- Skip a shared helper process unless the user explicitly added its executable path to a rule.
- The first version includes helper processes only when the user explicitly adds their executable paths; it never infers helper ownership from parent-child relationships.
- Restore only sessions whose `MutedByTool` value is true and whose `OriginalMute` value is false.
- User-facing UI, menus, dialogs, and errors use Simplified Chinese. Keep English technical terms only with Chinese explanation.
- Target idle working set below 80 MB as an optimization goal, with measured evidence before release.
- No placeholder implementation, no `TODO`, and no silent failure when no application rule is configured.

## Review Focus

These are the failure modes most likely to hurt a user and each one has a test or manual verification task:

- A selected app has no audio session yet: the app must stay functional, not crash, and scan again when a session appears.
- Two instances of the same EXE have different PIDs: both sessions must be muted.
- A helper process cannot be proven to belong to the selected app: it must be skipped, not muted.
- UAC is cancelled during admin handoff: the ordinary instance must resume without leaving a second manager active.
- The default output changes to Bluetooth, a headset, or a virtual device: enabled DND must still find matching sessions on active endpoints.
- The application is force-closed: the recovery journal must restore previously changed sessions on the next launch.

---

### Task 1: Bootstrap the Repository, SDK, Solution, and Test Projects

**Files:**
- Create: `.gitignore`
- Create: `global.json`
- Create: `Directory.Build.props`
- Create: `ChatDND.sln`
- Create: `src/ChatDND.Core/ChatDND.Core.csproj`
- Create: `src/ChatDND.Core/Marker.cs`
- Create: `src/ChatDND.App/ChatDND.App.csproj`
- Create: `tests/ChatDND.Core.Tests/ChatDND.Core.Tests.csproj`
- Create: `tests/ChatDND.Core.Tests/BootstrapTests.cs`

**Interfaces:**
- Consumes: none.
- Produces: a buildable solution with `ChatDND.Core` and `ChatDND.Core.Tests`; later tasks add files to these projects.

- [ ] **Step 1: Check for the .NET SDK and install a user-local SDK if needed**

Run from PowerShell:

```powershell
$sdks = @(dotnet --list-sdks)
if ($sdks.Count -eq 0) {
    $installDir = Join-Path $env:USERPROFILE '.dotnet'
    $installer = Join-Path $env:TEMP 'dotnet-install.ps1'
    Invoke-WebRequest 'https://dot.net/v1/dotnet-install.ps1' -OutFile $installer
    & $installer -Channel 10.0 -Quality GA -InstallDir $installDir
    $env:PATH = "$installDir;$env:PATH"
}
dotnet --list-sdks
```

Expected: at least one `10.0.x` SDK is printed.

- [ ] **Step 2: Create the repository metadata**

Create `.gitignore` with:

```gitignore
.vs/
.idea/
bin/
obj/
.dotnet/
TestResults/
*.user
*.suo
logs/
```

Create `global.json` with:

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature",
    "allowPrerelease": false
  }
}
```

Create `Directory.Build.props` with:

```xml
<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
</Project>
```

- [ ] **Step 3: Create the solution and projects**

Run:

```powershell
git init
dotnet new sln -n ChatDND
dotnet new classlib -n ChatDND.Core -o src/ChatDND.Core -f net10.0
dotnet new winforms -n ChatDND.App -o src/ChatDND.App -f net10.0
dotnet new xunit -n ChatDND.Core.Tests -o tests/ChatDND.Core.Tests -f net10.0
dotnet sln ChatDND.sln add src/ChatDND.Core/ChatDND.Core.csproj
dotnet sln ChatDND.sln add src/ChatDND.App/ChatDND.App.csproj
dotnet sln ChatDND.sln add tests/ChatDND.Core.Tests/ChatDND.Core.Tests.csproj
dotnet add src/ChatDND.App/ChatDND.App.csproj reference src/ChatDND.Core/ChatDND.Core.csproj
dotnet add tests/ChatDND.Core.Tests/ChatDND.Core.Tests.csproj reference src/ChatDND.Core/ChatDND.Core.csproj
dotnet add src/ChatDND.Core/ChatDND.Core.csproj package NAudio --version 2.2.1
```

Change both `ChatDND.Core.csproj` and `ChatDND.Core.Tests.csproj` `TargetFramework` to `net10.0-windows`. Add `<UseWindowsForms>true</UseWindowsForms>` to `ChatDND.Core.Tests.csproj` because Task 9 tests the Chinese UI constants in the WinForms executable.

- [ ] **Step 4: Add the bootstrap test**

Create `src/ChatDND.Core/Marker.cs`:

```csharp
namespace ChatDND.Core;

public sealed class Marker;
```

Create `tests/ChatDND.Core.Tests/BootstrapTests.cs`:

```csharp
using ChatDND.Core;

namespace ChatDND.Core.Tests;

public sealed class BootstrapTests
{
    [Fact]
    public void CoreAssemblyLoads()
    {
        Assert.Equal("ChatDND.Core", typeof(Marker).Assembly.GetName().Name);
    }
}
```

- [ ] **Step 5: Run the test suite**

Run:

```powershell
dotnet test ChatDND.sln
```

Expected: PASS with one test.

- [ ] **Step 6: Commit**

Run:

```powershell
git add .
git commit -m "chore: bootstrap ChatDND solution"
```

---

### Task 2: Add Domain Models, Path Normalization, and Rule Matching

**Files:**
- Create: `src/ChatDND.Core/Models/AppRule.cs`
- Create: `src/ChatDND.Core/Models/SessionKey.cs`
- Create: `src/ChatDND.Core/Models/SessionPlaybackState.cs`
- Create: `src/ChatDND.Core/Models/AudioSessionSnapshot.cs`
- Create: `src/ChatDND.Core/Matching/ProcessPathNormalizer.cs`
- Create: `src/ChatDND.Core/Matching/AppRuleMatcher.cs`
- Create: `tests/ChatDND.Core.Tests/Matching/ProcessPathNormalizerTests.cs`
- Create: `tests/ChatDND.Core.Tests/Matching/AppRuleMatcherTests.cs`

**Interfaces:**
- Consumes: `ChatDND.Core.Marker` only as assembly seed.
- Produces: `AppRule`, `SessionKey`, `SessionPlaybackState`, `AudioSessionSnapshot`, `ProcessPathNormalizer.Normalize`, `ProcessPathNormalizer.TryNormalize`, and `AppRuleMatcher.Match`.

- [ ] **Step 1: Write the failing normalization and matching tests**

Create `tests/ChatDND.Core.Tests/Matching/ProcessPathNormalizerTests.cs`:

```csharp
using ChatDND.Core.Matching;

namespace ChatDND.Core.Tests.Matching;

public sealed class ProcessPathNormalizerTests
{
    [Theory]
    [InlineData(" C:\\Program Files\\Tencent\\WeChat\\WeChat.exe ")]
    [InlineData("\"C:\\Program Files\\Tencent\\WeChat\\WeChat.exe\"")]
    public void Normalize_RemovesQuotesAndWhitespace(string input)
    {
        var result = ProcessPathNormalizer.Normalize(input);

        Assert.Equal(@"C:\Program Files\Tencent\WeChat\WeChat.exe", result);
    }

    [Fact]
    public void TryNormalize_ReturnsFalseForEmptyPath()
    {
        var result = ProcessPathNormalizer.TryNormalize(" ", out var normalized);

        Assert.False(result);
        Assert.Equal(string.Empty, normalized);
    }

    [Fact]
    public void TryNormalize_ReturnsFalseForInvalidPath()
    {
        var result = ProcessPathNormalizer.TryNormalize(
            "C:\\invalid\0path.exe",
            out var normalized);

        Assert.False(result);
        Assert.Equal(string.Empty, normalized);
    }

    [Fact]
    public void TryNormalize_ReturnsFalseForBareFilename()
    {
        var result = ProcessPathNormalizer.TryNormalize(
            "WeChat.exe",
            out var normalized);

        Assert.False(result);
        Assert.Equal(string.Empty, normalized);
    }
}
```

Create `tests/ChatDND.Core.Tests/Matching/AppRuleMatcherTests.cs`:

```csharp
using ChatDND.Core.Matching;
using ChatDND.Core.Models;

namespace ChatDND.Core.Tests.Matching;

public sealed class AppRuleMatcherTests
{
    private static readonly AppRule WeChat = new(
        "wechat",
        "微信",
        [@"C:\Program Files\Tencent\WeChat\WeChat.exe"]);

    [Fact]
    public void Match_UsesNormalizedFullPath()
    {
        var result = AppRuleMatcher.Match(
            @"c:\program files\tencent\wechat\wechat.exe",
            [WeChat]);

        Assert.Same(WeChat, result);
    }

    [Fact]
    public void Match_MatchesBareFilenameAgainstConfiguredRule()
    {
        var result = AppRuleMatcher.Match("WeChat.exe", [WeChat]);

        Assert.Same(WeChat, result);
    }

    [Fact]
    public void Match_DoesNotMatchAnotherApplicationWithSameFilename()
    {
        var result = AppRuleMatcher.Match(
            @"D:\Portable\Other\WeChat.exe",
            [WeChat]);

        Assert.Null(result);
    }

    [Fact]
    public void Match_IgnoresDisabledRules()
    {
        var disabled = WeChat with { Enabled = false };

        var result = AppRuleMatcher.Match(
            @"C:\Program Files\Tencent\WeChat\WeChat.exe",
            [disabled]);

        Assert.Null(result);
    }

    [Fact]
    public void Match_DoesNotMatchUnlistedHelperProcess()
    {
        var result = AppRuleMatcher.Match(
            @"C:\Program Files\Common Files\SharedAudio.exe",
            [WeChat]);

        Assert.Null(result);
    }

    [Fact]
    public void Match_ReturnsNullForNullRules()
    {
        var result = AppRuleMatcher.Match("WeChat.exe", null);

        Assert.Null(result);
    }
}
```

- [ ] **Step 2: Run the tests and verify they fail**

Run:

```powershell
dotnet test tests/ChatDND.Core.Tests/ChatDND.Core.Tests.csproj --filter FullyQualifiedName~Matching
```

Expected: compile failure because the model and matcher types do not exist.

- [ ] **Step 3: Add the domain models and matcher**

Create `src/ChatDND.Core/Models/AppRule.cs`:

```csharp
namespace ChatDND.Core.Models;

public sealed record AppRule(
    string Id,
    string DisplayName,
    IReadOnlyList<string> ExecutablePaths,
    bool Enabled = true);
```

Create `src/ChatDND.Core/Models/SessionKey.cs`:

```csharp
namespace ChatDND.Core.Models;

public readonly record struct SessionKey(
    string SessionIdentifier,
    string SessionInstanceIdentifier,
    uint ProcessId);
```

Create `src/ChatDND.Core/Models/SessionPlaybackState.cs`:

```csharp
namespace ChatDND.Core.Models;

public enum SessionPlaybackState
{
    Inactive,
    Active,
    Expired
}
```

Create `src/ChatDND.Core/Models/AudioSessionSnapshot.cs`:

```csharp
namespace ChatDND.Core.Models;

public sealed record AudioSessionSnapshot(
    SessionKey Key,
    string ProcessPath,
    bool IsMuted,
    SessionPlaybackState State);
```

Create `src/ChatDND.Core/Matching/ProcessPathNormalizer.cs`:

```csharp
namespace ChatDND.Core.Matching;

public static class ProcessPathNormalizer
{
    public static string Normalize(string path)
    {
        if (!TryNormalize(path, out var normalized))
        {
            throw new ArgumentException(
                "Process path must be a rooted, valid path.",
                nameof(path));
        }

        return normalized;
    }

    public static bool TryNormalize(string? path, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var trimmed = path.Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return false;
        }

        try
        {
            if (!Path.IsPathRooted(trimmed))
            {
                return false;
            }

            normalized = Path.GetFullPath(trimmed);
            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException
            or NotSupportedException
            or PathTooLongException)
        {
            normalized = string.Empty;
            return false;
        }
    }
}
```

Create `src/ChatDND.Core/Matching/AppRuleMatcher.cs`:

```csharp
using ChatDND.Core.Models;

namespace ChatDND.Core.Matching;

public static class AppRuleMatcher
{
    public static AppRule? Match(
        string? processPath,
        IEnumerable<AppRule>? rules)
    {
        if (string.IsNullOrWhiteSpace(processPath) || rules is null)
        {
            return null;
        }

        var processPathNormalized = ProcessPathNormalizer.TryNormalize(
            processPath,
            out var normalizedPath);
        var processFileName = GetFileName(processPath);

        foreach (var rule in rules.Where(rule => rule is { Enabled: true }))
        {
            if (rule.ExecutablePaths is null || rule.ExecutablePaths.Count == 0)
            {
                continue;
            }

            foreach (var candidatePath in rule.ExecutablePaths)
            {
                if (processPathNormalized
                    && ProcessPathNormalizer.TryNormalize(candidatePath, out var normalizedRulePath)
                    && string.Equals(
                        normalizedPath,
                        normalizedRulePath,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return rule;
                }

                if (!processPathNormalized
                    && string.Equals(
                        processFileName,
                        GetFileName(candidatePath),
                        StringComparison.OrdinalIgnoreCase))
                {
                    return rule;
                }
            }
        }

        return null;
    }

    private static string? GetFileName(string path)
    {
        try
        {
            return Path.GetFileName(path);
        }
        catch (Exception exception) when (
            exception is ArgumentException
            or NotSupportedException
            or PathTooLongException)
        {
            return null;
        }
    }
}
```

- [ ] **Step 4: Run the tests and verify they pass**

Run:

```powershell
dotnet test tests/ChatDND.Core.Tests/ChatDND.Core.Tests.csproj --filter FullyQualifiedName~Matching
```

Expected: PASS for normalization and matching tests.

- [ ] **Step 5: Commit**

Run:

```powershell
git add src/ChatDND.Core tests/ChatDND.Core.Tests
git commit -m "feat: add app rule matching"
```

---

### Task 3: Add Settings and Rule Persistence

**Files:**
- Create: `src/ChatDND.Core/Models/DndSettings.cs`
- Create: `src/ChatDND.Core/Services/AppRuleStore.cs`
- Create: `tests/ChatDND.Core.Tests/Services/AppRuleStoreTests.cs`

**Interfaces:**
- Consumes: `AppRule` from Task 2.
- Produces: `DndSettings` with `AutoEnableOnLaunch`, `MinimizeToTrayOnStartup`, `RunAsAdministratorAtStartup`, `ScanIntervalMs`, and `Rules`; `AppRuleStore.Load()` and `AppRuleStore.Save(DndSettings)`.

- [ ] **Step 1: Write failing persistence tests**

Create `tests/ChatDND.Core.Tests/Services/AppRuleStoreTests.cs`:

```csharp
using ChatDND.Core.Models;
using ChatDND.Core.Services;

namespace ChatDND.Core.Tests.Services;

public sealed class AppRuleStoreTests
{
    [Fact]
    public void SaveAndLoad_RoundTripsRules()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "settings.json");
        var store = new AppRuleStore(path);
        var settings = DndSettings.Default with
        {
            AutoEnableOnLaunch = false,
            Rules =
            [
                new AppRule(
                    "wechat",
                    "微信",
                    [@"C:\Program Files\Tencent\WeChat\WeChat.exe"])
            ]
        };

        store.Save(settings);
        var loaded = store.Load();

        Assert.False(loaded.AutoEnableOnLaunch);
        Assert.Single(loaded.Rules);
        Assert.Equal("wechat", loaded.Rules[0].Id);
    }

    [Fact]
    public void Load_WhenJsonIsCorrupt_BacksUpAndReturnsDefaults()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "settings.json");
        File.WriteAllText(path, "{ broken json");
        var store = new AppRuleStore(path);

        var loaded = store.Load();

        Assert.True(loaded.AutoEnableOnLaunch);
        Assert.Equal(500, loaded.ScanIntervalMs);
        Assert.Empty(loaded.Rules);
        Assert.True(Directory.GetFiles(directory, "settings.json.corrupt-*").Length == 1);
    }

    [Fact]
    public void Load_ClampsInvalidScanInterval()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "settings.json");
        File.WriteAllText(
            path,
            """
            {
              "autoEnableOnLaunch": true,
              "minimizeToTrayOnStartup": true,
              "runAsAdministratorAtStartup": false,
              "scanIntervalMs": 0,
              "rules": []
            }
            """);

        var loaded = new AppRuleStore(path).Load();

        Assert.Equal(100, loaded.ScanIntervalMs);
    }
}
```

- [ ] **Step 2: Run the tests and verify they fail**

Run:

```powershell
dotnet test tests/ChatDND.Core.Tests/ChatDND.Core.Tests.csproj --filter FullyQualifiedName~AppRuleStoreTests
```

Expected: compile failure because `DndSettings` and `AppRuleStore` do not exist.

- [ ] **Step 3: Add settings and the JSON store**

Create `src/ChatDND.Core/Models/DndSettings.cs`:

```csharp
namespace ChatDND.Core.Models;

public sealed record DndSettings(
    bool AutoEnableOnLaunch,
    bool MinimizeToTrayOnStartup,
    bool RunAsAdministratorAtStartup,
    int ScanIntervalMs,
    IReadOnlyList<AppRule> Rules)
{
    public static DndSettings Default { get; } = new(
        AutoEnableOnLaunch: true,
        MinimizeToTrayOnStartup: true,
        RunAsAdministratorAtStartup: false,
        ScanIntervalMs: 500,
        Rules: []);
}
```

Create `src/ChatDND.Core/Services/AppRuleStore.cs`:

```csharp
using System.Text.Json;
using ChatDND.Core.Models;

namespace ChatDND.Core.Services;

public sealed class AppRuleStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public AppRuleStore(string settingsPath)
    {
        SettingsPath = settingsPath;
    }

    public string SettingsPath { get; }

    public DndSettings Load()
    {
        if (!File.Exists(SettingsPath))
        {
            return DndSettings.Default;
        }

        try
        {
            var json = File.ReadAllText(SettingsPath);
            return Normalize(
                JsonSerializer.Deserialize<DndSettings>(json, Options)
                    ?? DndSettings.Default);
        }
        catch (JsonException)
        {
            var backupPath = $"{SettingsPath}.corrupt-{DateTime.UtcNow:yyyyMMddHHmmss}";
            File.Move(SettingsPath, backupPath, overwrite: true);
            return DndSettings.Default;
        }
    }

    public void Save(DndSettings settings)
    {
        var directory = Path.GetDirectoryName(SettingsPath)
            ?? throw new InvalidOperationException("Settings path has no directory.");
        Directory.CreateDirectory(directory);

        var temporaryPath = $"{SettingsPath}.tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, Options));
        File.Move(temporaryPath, SettingsPath, overwrite: true);
    }

    private static DndSettings Normalize(DndSettings settings)
    {
        return settings with
        {
            ScanIntervalMs = Math.Clamp(settings.ScanIntervalMs, 100, 5000),
            Rules = settings.Rules ?? []
        };
    }
}
```

- [ ] **Step 4: Run the tests and verify they pass**

Run:

```powershell
dotnet test tests/ChatDND.Core.Tests/ChatDND.Core.Tests.csproj --filter FullyQualifiedName~AppRuleStoreTests
```

Expected: PASS.

- [ ] **Step 5: Commit**

Run:

```powershell
git add src/ChatDND.Core tests/ChatDND.Core.Tests
git commit -m "feat: persist DND settings and app rules"
```

---

### Task 4: Add Audio Abstractions and the DND Coordinator

**Files:**
- Create: `src/ChatDND.Core/Audio/IAudioSessionProvider.cs`
- Create: `src/ChatDND.Core/Audio/IAudioSessionController.cs`
- Create: `src/ChatDND.Core/Services/IRecoveryJournal.cs`
- Create: `src/ChatDND.Core/Services/DndCoordinator.cs`
- Create: `src/ChatDND.Core/Models/RecoveryRecord.cs`
- Create: `src/ChatDND.Core/Models/DndEnableResult.cs`
- Create: `tests/ChatDND.Core.Tests/Services/Fakes/FakeAudioSessionProvider.cs`
- Create: `tests/ChatDND.Core.Tests/Services/Fakes/FakeAudioSessionController.cs`
- Create: `tests/ChatDND.Core.Tests/Services/Fakes/FakeRecoveryJournal.cs`
- Create: `tests/ChatDND.Core.Tests/Services/DndCoordinatorTests.cs`

**Interfaces:**
- Consumes: `AppRule`, `AudioSessionSnapshot`, `SessionKey`, `AppRuleMatcher`, `DndSettings`.
- Produces: `IAudioSessionProvider`, `IAudioSessionController`, `IRecoveryJournal`, `DndCoordinator.Enable`, `DndCoordinator.Tick`, `DndCoordinator.Disable`, `DndCoordinator.IsEnabled`.

- [ ] **Step 1: Write failing coordinator tests**

Create `tests/ChatDND.Core.Tests/Services/Fakes/FakeAudioSessionProvider.cs`:

```csharp
using ChatDND.Core.Audio;
using ChatDND.Core.Models;

namespace ChatDND.Core.Tests.Services.Fakes;

internal sealed class FakeAudioSessionProvider : IAudioSessionProvider
{
    public List<AudioSessionSnapshot> Sessions { get; } = [];

    public IReadOnlyList<AudioSessionSnapshot> GetSessions() => Sessions.ToArray();
}
```

Create `tests/ChatDND.Core.Tests/Services/Fakes/FakeAudioSessionController.cs`:

```csharp
using ChatDND.Core.Audio;
using ChatDND.Core.Models;

namespace ChatDND.Core.Tests.Services.Fakes;

internal sealed class FakeAudioSessionController : IAudioSessionController
{
    public Dictionary<SessionKey, bool> MuteStates { get; } = [];

    public bool TrySetMute(SessionKey sessionKey, bool muted)
    {
        MuteStates[sessionKey] = muted;
        var session = Provider.Sessions.FirstOrDefault(item => item.Key == sessionKey);
        if (session is not null)
        {
            Provider.Sessions[Provider.Sessions.IndexOf(session)] =
                session with { IsMuted = muted };
        }

        return session is not null;
    }

    public FakeAudioSessionProvider Provider { get; init; } = new();
}
```

Create `tests/ChatDND.Core.Tests/Services/Fakes/FakeRecoveryJournal.cs`:

```csharp
using ChatDND.Core.Models;
using ChatDND.Core.Services;

namespace ChatDND.Core.Tests.Services.Fakes;

internal sealed class FakeRecoveryJournal : IRecoveryJournal
{
    public List<RecoveryRecord> Records { get; private set; } = [];

    public IReadOnlyList<RecoveryRecord> Load() => Records;

    public void Save(IReadOnlyCollection<RecoveryRecord> records)
    {
        Records = [.. records];
    }

    public void Clear()
    {
        Records = [];
    }
}
```

Create `tests/ChatDND.Core.Tests/Services/DndCoordinatorTests.cs`:

```csharp
using ChatDND.Core.Models;
using ChatDND.Core.Services;
using ChatDND.Core.Tests.Services.Fakes;

namespace ChatDND.Core.Tests.Services;

public sealed class DndCoordinatorTests
{
    private static readonly AppRule WeChat = new(
        "wechat",
        "微信",
        [@"C:\Program Files\Tencent\WeChat\WeChat.exe"]);

    [Fact]
    public void Enable_MutesEveryMatchingInstance()
    {
        var provider = new FakeAudioSessionProvider();
        var controller = new FakeAudioSessionController { Provider = provider };
        var first = Session(@"C:\Program Files\Tencent\WeChat\WeChat.exe", 101);
        var second = Session(@"C:\Program Files\Tencent\WeChat\WeChat.exe", 202);
        provider.Sessions.AddRange([first, second]);
        var coordinator = new DndCoordinator(
            provider,
            controller,
            new FakeRecoveryJournal());

        var result = coordinator.Enable([WeChat]);

        Assert.Equal(DndEnableResult.Enabled, result);
        Assert.True(controller.MuteStates[first.Key]);
        Assert.True(controller.MuteStates[second.Key]);
    }

    [Fact]
    public void Enable_WithNoRules_DoesNotEnterDnd()
    {
        var provider = new FakeAudioSessionProvider();
        var coordinator = new DndCoordinator(
            provider,
            new FakeAudioSessionController { Provider = provider },
            new FakeRecoveryJournal());

        var result = coordinator.Enable([]);

        Assert.Equal(DndEnableResult.NoRules, result);
        Assert.False(coordinator.IsEnabled);
    }

    [Fact]
    public void Disable_RestoresOnlyOriginallyUnmutedSessions()
    {
        var provider = new FakeAudioSessionProvider();
        var controller = new FakeAudioSessionController { Provider = provider };
        var originallyMuted = Session(
            @"C:\Program Files\Tencent\WeChat\WeChat.exe",
            101,
            isMuted: true);
        var originallyAudible = Session(
            @"C:\Program Files\Tencent\WeChat\WeChat.exe",
            202,
            isMuted: false);
        provider.Sessions.AddRange([originallyMuted, originallyAudible]);
        var coordinator = new DndCoordinator(
            provider,
            controller,
            new FakeRecoveryJournal());
        coordinator.Enable([WeChat]);

        coordinator.Disable();

        Assert.True(controller.MuteStates[originallyMuted.Key]);
        Assert.False(controller.MuteStates[originallyAudible.Key]);
    }

    [Fact]
    public void Tick_MutesSessionAppearingAfterEnable()
    {
        var provider = new FakeAudioSessionProvider();
        var controller = new FakeAudioSessionController { Provider = provider };
        var coordinator = new DndCoordinator(
            provider,
            controller,
            new FakeRecoveryJournal());
        coordinator.Enable([WeChat]);
        var laterSession = Session(
            @"C:\Program Files\Tencent\WeChat\WeChat.exe",
            303);
        provider.Sessions.Add(laterSession);

        coordinator.Tick([WeChat]);

        Assert.True(controller.MuteStates[laterSession.Key]);
    }

    private static AudioSessionSnapshot Session(
        string processPath,
        uint processId,
        bool isMuted = false)
    {
        return new AudioSessionSnapshot(
            new SessionKey($"session-{processId}", $"instance-{processId}", processId),
            processPath,
            isMuted,
            SessionPlaybackState.Active);
    }
}
```

- [ ] **Step 2: Run the tests and verify they fail**

Run:

```powershell
dotnet test tests/ChatDND.Core.Tests/ChatDND.Core.Tests.csproj --filter FullyQualifiedName~DndCoordinatorTests
```

Expected: compile failure because coordinator interfaces and types do not exist.

- [ ] **Step 3: Add the coordinator contracts and implementation**

Create `src/ChatDND.Core/Audio/IAudioSessionProvider.cs`:

```csharp
using ChatDND.Core.Models;

namespace ChatDND.Core.Audio;

public interface IAudioSessionProvider
{
    IReadOnlyList<AudioSessionSnapshot> GetSessions();
}
```

Create `src/ChatDND.Core/Audio/IAudioSessionController.cs`:

```csharp
using ChatDND.Core.Models;

namespace ChatDND.Core.Audio;

public interface IAudioSessionController
{
    bool TrySetMute(SessionKey sessionKey, bool muted);
}
```

Create `src/ChatDND.Core/Models/RecoveryRecord.cs`:

```csharp
namespace ChatDND.Core.Models;

public sealed record RecoveryRecord(
    SessionKey Key,
    string ProcessPath,
    bool OriginalMute,
    bool MutedByTool);
```

Create `src/ChatDND.Core/Models/DndEnableResult.cs`:

```csharp
namespace ChatDND.Core.Models;

public enum DndEnableResult
{
    Enabled,
    NoRules
}
```

Create `src/ChatDND.Core/Services/IRecoveryJournal.cs`:

```csharp
using ChatDND.Core.Models;

namespace ChatDND.Core.Services;

public interface IRecoveryJournal
{
    IReadOnlyList<RecoveryRecord> Load();

    void Save(IReadOnlyCollection<RecoveryRecord> records);

    void Clear();
}
```

Create `src/ChatDND.Core/Services/DndCoordinator.cs`:

```csharp
using ChatDND.Core.Audio;
using ChatDND.Core.Matching;
using ChatDND.Core.Models;

namespace ChatDND.Core.Services;

public sealed class DndCoordinator
{
    private readonly IAudioSessionProvider _provider;
    private readonly IAudioSessionController _controller;
    private readonly IRecoveryJournal _journal;
    private readonly Dictionary<SessionKey, RecoveryRecord> _managed = [];

    public DndCoordinator(
        IAudioSessionProvider provider,
        IAudioSessionController controller,
        IRecoveryJournal journal)
    {
        _provider = provider;
        _controller = controller;
        _journal = journal;
    }

    public bool IsEnabled { get; private set; }

    public DndEnableResult Enable(IReadOnlyList<AppRule> rules)
    {
        if (IsEnabled)
        {
            return DndEnableResult.Enabled;
        }

        if (rules.Count == 0 || !rules.Any(rule => rule.Enabled))
        {
            return DndEnableResult.NoRules;
        }

        IsEnabled = true;
        ApplyRules(rules);
        return DndEnableResult.Enabled;
    }

    public void Tick(IReadOnlyList<AppRule> rules)
    {
        if (IsEnabled)
        {
            ApplyRules(rules);
        }
    }

    public void Disable()
    {
        if (!IsEnabled)
        {
            return;
        }

        IsEnabled = false;
        var sessions = _provider.GetSessions()
            .GroupBy(item => item.Key)
            .ToDictionary(group => group.Key, group => group.First());
        var remainingRecords = new List<RecoveryRecord>();
        foreach (var record in _managed.Values)
        {
            if (sessions.ContainsKey(record.Key)
                && record.MutedByTool
                && !record.OriginalMute)
            {
                if (!_controller.TrySetMute(record.Key, muted: false))
                {
                    remainingRecords.Add(record);
                }
            }
        }

        _managed.Clear();
        if (remainingRecords.Count == 0)
        {
            _journal.Clear();
        }
        else
        {
            _journal.Save(remainingRecords);
        }
    }

    private void ApplyRules(IReadOnlyList<AppRule> rules)
    {
        var sessions = _provider.GetSessions();
        var liveKeys = sessions.Select(item => item.Key).ToHashSet();

        foreach (var session in sessions)
        {
            if (AppRuleMatcher.Match(session.ProcessPath, rules) is null)
            {
                continue;
            }

            if (!_managed.TryGetValue(session.Key, out var record))
            {
                if (session.IsMuted)
                {
                    _managed[session.Key] = new RecoveryRecord(
                        session.Key,
                        session.ProcessPath,
                        OriginalMute: true,
                        MutedByTool: false);
                    continue;
                }

                if (_controller.TrySetMute(session.Key, muted: true))
                {
                    _managed[session.Key] = new RecoveryRecord(
                        session.Key,
                        session.ProcessPath,
                        OriginalMute: false,
                        MutedByTool: true);
                }

                continue;
            }

            if (record.MutedByTool && !session.IsMuted)
            {
                _controller.TrySetMute(session.Key, muted: true);
            }
        }

        foreach (var key in _managed.Keys.Where(key => !liveKeys.Contains(key)).ToArray())
        {
            _managed.Remove(key);
        }

        _journal.Save(_managed.Values.ToArray());
    }
}
```

- [ ] **Step 4: Run the tests and verify they pass**

Run:

```powershell
dotnet test tests/ChatDND.Core.Tests/ChatDND.Core.Tests.csproj --filter FullyQualifiedName~DndCoordinatorTests
```

Expected: PASS for all coordinator tests.

- [ ] **Step 5: Commit**

Run:

```powershell
git add src/ChatDND.Core tests/ChatDND.Core.Tests
git commit -m "feat: add DND coordinator"
```

---

### Task 5: Add Recovery Journal and Crash Recovery

**Files:**
- Create: `src/ChatDND.Core/Services/JsonRecoveryJournal.cs`
- Create: `src/ChatDND.Core/Services/RecoveryService.cs`
- Create: `tests/ChatDND.Core.Tests/Services/JsonRecoveryJournalTests.cs`
- Create: `tests/ChatDND.Core.Tests/Services/RecoveryServiceTests.cs`

**Interfaces:**
- Consumes: `IRecoveryJournal`, `RecoveryRecord`, `IAudioSessionProvider`, `IAudioSessionController`.
- Produces: `JsonRecoveryJournal`, `RecoveryService.Recover()`.

- [ ] **Step 1: Write failing recovery tests**

Create `tests/ChatDND.Core.Tests/Services/JsonRecoveryJournalTests.cs`:

```csharp
using ChatDND.Core.Models;
using ChatDND.Core.Services;

namespace ChatDND.Core.Tests.Services;

public sealed class JsonRecoveryJournalTests
{
    [Fact]
    public void SaveLoadAndClear_RoundTripsRecords()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var journal = new JsonRecoveryJournal(Path.Combine(directory, "recovery.json"));
        var record = new RecoveryRecord(
            new SessionKey("session", "instance", 42),
            @"C:\Apps\Chat.exe",
            OriginalMute: false,
            MutedByTool: true);

        journal.Save([record]);
        var loaded = journal.Load();
        journal.Clear();

        Assert.Single(loaded);
        Assert.False(loaded[0].OriginalMute);
        Assert.Empty(journal.Load());
    }
}
```

Create `tests/ChatDND.Core.Tests/Services/RecoveryServiceTests.cs`:

```csharp
using ChatDND.Core.Models;
using ChatDND.Core.Services;
using ChatDND.Core.Tests.Services.Fakes;

namespace ChatDND.Core.Tests.Services;

public sealed class RecoveryServiceTests
{
    [Fact]
    public void Recover_UnmutesOnlySessionsChangedByTheTool()
    {
        var provider = new FakeAudioSessionProvider();
        var controller = new FakeAudioSessionController { Provider = provider };
        var key = new SessionKey("session", "instance", 42);
        provider.Sessions.Add(new AudioSessionSnapshot(
            key,
            @"C:\Apps\Chat.exe",
            IsMuted: true,
            SessionPlaybackState.Active));
        var journal = new FakeRecoveryJournal();
        journal.Save([
            new RecoveryRecord(key, @"C:\Apps\Chat.exe", OriginalMute: false, MutedByTool: true)
        ]);
        var service = new RecoveryService(provider, controller, journal);

        service.Recover();

        Assert.False(controller.MuteStates[key]);
        Assert.Empty(journal.Load());
    }

    [Fact]
    public void Recover_LeavesOriginallyMutedSessionsMuted()
    {
        var provider = new FakeAudioSessionProvider();
        var controller = new FakeAudioSessionController { Provider = provider };
        var key = new SessionKey("session", "instance", 42);
        provider.Sessions.Add(new AudioSessionSnapshot(
            key,
            @"C:\Apps\Chat.exe",
            IsMuted: true,
            SessionPlaybackState.Active));
        var journal = new FakeRecoveryJournal();
        journal.Save([
            new RecoveryRecord(key, @"C:\Apps\Chat.exe", OriginalMute: true, MutedByTool: true)
        ]);

        new RecoveryService(provider, controller, journal).Recover();

        Assert.Empty(controller.MuteStates);
        Assert.Empty(journal.Load());
    }
}
```

- [ ] **Step 2: Run the tests and verify they fail**

Run:

```powershell
dotnet test tests/ChatDND.Core.Tests/ChatDND.Core.Tests.csproj --filter "FullyQualifiedName~Recovery"
```

Expected: compile failure because recovery implementations do not exist.

- [ ] **Step 3: Implement the JSON journal and recovery service**

Create `src/ChatDND.Core/Services/JsonRecoveryJournal.cs`:

```csharp
using System.Text.Json;
using ChatDND.Core.Models;

namespace ChatDND.Core.Services;

public sealed class JsonRecoveryJournal : IRecoveryJournal
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly string _path;

    public JsonRecoveryJournal(string path)
    {
        _path = path;
    }

    public IReadOnlyList<RecoveryRecord> Load()
    {
        if (!File.Exists(_path))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<RecoveryRecord[]>(json, Options) ?? [];
        }
        catch (JsonException)
        {
            File.Move(
                _path,
                $"{_path}.corrupt-{DateTime.UtcNow:yyyyMMddHHmmss}",
                overwrite: true);
            return [];
        }
    }

    public void Save(IReadOnlyCollection<RecoveryRecord> records)
    {
        var directory = Path.GetDirectoryName(_path)
            ?? throw new InvalidOperationException("Recovery path has no directory.");
        Directory.CreateDirectory(directory);
        var temporaryPath = $"{_path}.tmp";
        File.WriteAllText(
            temporaryPath,
            JsonSerializer.Serialize(records, Options));
        File.Move(temporaryPath, _path, overwrite: true);
    }

    public void Clear()
    {
        if (File.Exists(_path))
        {
            File.Delete(_path);
        }
    }
}
```

Create `src/ChatDND.Core/Services/RecoveryService.cs`:

```csharp
using ChatDND.Core.Audio;

namespace ChatDND.Core.Services;

public sealed class RecoveryService
{
    private readonly IAudioSessionProvider _provider;
    private readonly IAudioSessionController _controller;
    private readonly IRecoveryJournal _journal;

    public RecoveryService(
        IAudioSessionProvider provider,
        IAudioSessionController controller,
        IRecoveryJournal journal)
    {
        _provider = provider;
        _controller = controller;
        _journal = journal;
    }

    public void Recover()
    {
        var records = _journal.Load();
        if (records.Count == 0)
        {
            return;
        }

        var liveKeys = _provider.GetSessions()
            .Select(session => session.Key)
            .ToHashSet();
        var remainingRecords = new List<RecoveryRecord>();

        foreach (var record in records)
        {
            if (liveKeys.Contains(record.Key)
                && record.MutedByTool
                && !record.OriginalMute)
            {
                if (!_controller.TrySetMute(record.Key, muted: false))
                {
                    remainingRecords.Add(record);
                }
            }
        }

        if (remainingRecords.Count == 0)
        {
            _journal.Clear();
        }
        else
        {
            _journal.Save(remainingRecords);
        }
    }
}
```

- [ ] **Step 4: Run the tests and verify they pass**

Run:

```powershell
dotnet test tests/ChatDND.Core.Tests/ChatDND.Core.Tests.csproj --filter "FullyQualifiedName~Recovery"
```

Expected: PASS.

- [ ] **Step 5: Commit**

Run:

```powershell
git add src/ChatDND.Core tests/ChatDND.Core.Tests
git commit -m "feat: add crash recovery journal"
```

---

### Task 6: Add the NAudio WASAPI Adapter and Process Path Resolver

**Files:**
- Create: `src/ChatDND.Core/Interop/ProcessPathResolver.cs`
- Create: `src/ChatDND.Core/Logging/ILog.cs`
- Create: `src/ChatDND.Core/Audio/ICoreAudioSessionSource.cs`
- Create: `src/ChatDND.Core/Audio/CoreAudioSessionData.cs`
- Create: `src/ChatDND.Core/Audio/CoreAudioSessionMapper.cs`
- Create: `src/ChatDND.Core/Audio/NaudioCoreAudioSessionSource.cs`
- Create: `src/ChatDND.Core/Audio/WasapiAudioSessionProvider.cs`
- Create: `src/ChatDND.Core/Audio/WasapiAudioSessionController.cs`
- Create: `tests/ChatDND.Core.Tests/Audio/CoreAudioSessionMapperTests.cs`
- Create: `tests/ChatDND.Core.Tests/Interop/ProcessPathResolverTests.cs`

**Interfaces:**
- Consumes: `IAudioSessionProvider`, `IAudioSessionController`, `SessionKey`, `AudioSessionSnapshot`.
- Produces: `ProcessPathResolver.TryResolve(uint)`, `ILog`, `ICoreAudioSessionSource.Enumerate()`, `ICoreAudioSessionSource.TrySetMute(SessionKey, bool)`, `NaudioCoreAudioSessionSource`, `WasapiAudioSessionProvider`, `WasapiAudioSessionController`.

- [ ] **Step 1: Write failing adapter tests**

Create `tests/ChatDND.Core.Tests/Audio/CoreAudioSessionMapperTests.cs`:

```csharp
using ChatDND.Core.Audio;
using ChatDND.Core.Models;

namespace ChatDND.Core.Tests.Audio;

public sealed class CoreAudioSessionMapperTests
{
    [Fact]
    public void ToSnapshot_PreservesSessionIdentityAndMuteState()
    {
        var data = new CoreAudioSessionData(
            new SessionKey("session", "instance", 42),
            @"C:\Apps\Chat.exe",
            IsMuted: true,
            SessionPlaybackState.Active);

        var snapshot = CoreAudioSessionMapper.ToSnapshot(data);

        Assert.Equal(data.Key, snapshot.Key);
        Assert.Equal(data.ProcessPath, snapshot.ProcessPath);
        Assert.True(snapshot.IsMuted);
        Assert.Equal(SessionPlaybackState.Active, snapshot.State);
    }
}
```

Create `tests/ChatDND.Core.Tests/Interop/ProcessPathResolverTests.cs`:

```csharp
using ChatDND.Core.Interop;

namespace ChatDND.Core.Tests.Interop;

public sealed class ProcessPathResolverTests
{
    [Fact]
    public void TryResolve_ReturnsPathForCurrentProcess()
    {
        var processId = (uint)Environment.ProcessId;

        var path = ProcessPathResolver.TryResolve(processId);

        Assert.NotNull(path);
        Assert.True(path!.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
    }
}
```

- [ ] **Step 2: Run the tests and verify they fail**

Run:

```powershell
dotnet test tests/ChatDND.Core.Tests/ChatDND.Core.Tests.csproj --filter "FullyQualifiedName~CoreAudioSessionMapper|FullyQualifiedName~ProcessPathResolver"
```

Expected: compile failure because audio adapter and P/Invoke types do not exist.

- [ ] **Step 3: Implement the process path resolver**

Create `src/ChatDND.Core/Interop/ProcessPathResolver.cs`:

```csharp
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ChatDND.Core.Interop;

public static class ProcessPathResolver
{
    private const uint ProcessQueryLimitedInformation = 0x1000;

    public static string? TryResolve(uint processId)
    {
        var handle = OpenProcess(ProcessQueryLimitedInformation, false, (int)processId);
        if (handle == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            var capacity = 4096u;
            var buffer = new StringBuilder((int)capacity);
            return QueryFullProcessImageName(
                handle,
                0,
                buffer,
                ref capacity)
                ? buffer.ToString()
                : null;
        }
        catch (Exception) when (processId != 0)
        {
            return null;
        }
        finally
        {
            CloseHandle(handle);
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(
        uint desiredAccess,
        bool inheritHandle,
        int processId);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool QueryFullProcessImageName(
        IntPtr process,
        uint flags,
        StringBuilder executableName,
        ref uint size);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);
}
```

- [ ] **Step 4: Implement the Core Audio source and map**

Create `src/ChatDND.Core/Logging/ILog.cs`:

```csharp
namespace ChatDND.Core.Logging;

public interface ILog
{
    void Info(string message);

    void Warn(string message);

    void Error(string message);
}
```

Create `src/ChatDND.Core/Audio/CoreAudioSessionData.cs`:

```csharp
using ChatDND.Core.Models;

namespace ChatDND.Core.Audio;

public sealed record CoreAudioSessionData(
    SessionKey Key,
    string ProcessPath,
    bool IsMuted,
    SessionPlaybackState State);
```

Create `src/ChatDND.Core/Audio/ICoreAudioSessionSource.cs`:

```csharp
using ChatDND.Core.Models;

namespace ChatDND.Core.Audio;

public interface ICoreAudioSessionSource
{
    IReadOnlyList<CoreAudioSessionData> Enumerate();

    bool TrySetMute(SessionKey sessionKey, bool muted);
}
```

Create `src/ChatDND.Core/Audio/CoreAudioSessionMapper.cs`:

```csharp
using ChatDND.Core.Models;

namespace ChatDND.Core.Audio;

public static class CoreAudioSessionMapper
{
    public static AudioSessionSnapshot ToSnapshot(CoreAudioSessionData data)
    {
        return new AudioSessionSnapshot(
            data.Key,
            data.ProcessPath,
            data.IsMuted,
            data.State);
    }
}
```

Create `src/ChatDND.Core/Audio/NaudioCoreAudioSessionSource.cs`:

```csharp
using ChatDND.Core.Interop;
using ChatDND.Core.Logging;
using ChatDND.Core.Models;
using NAudio.CoreAudioApi;

namespace ChatDND.Core.Audio;

public sealed class NaudioCoreAudioSessionSource : ICoreAudioSessionSource
{
    private readonly ILog _log;

    public NaudioCoreAudioSessionSource(ILog log)
    {
        _log = log;
    }

    public IReadOnlyList<CoreAudioSessionData> Enumerate()
    {
        var result = new List<CoreAudioSessionData>();
        using var enumerator = new MMDeviceEnumerator();
        using var devices = enumerator.EnumerateAudioEndPoints(
            DataFlow.Render,
            DeviceState.Active);

        try
        {
            foreach (var device in devices)
            {
                using (device)
                {
                    var sessions = device.AudioSessionManager.Sessions;
                    for (var index = 0; index < sessions.Count; index++)
                    {
                        var session = sessions[index];
                        var processId = session.GetProcessID;
                        var processPath = ProcessPathResolver.TryResolve(processId)
                            ?? $"PID:{processId}";
                        var key = new SessionKey(
                            session.GetSessionIdentifier,
                            session.GetSessionInstanceIdentifier,
                            processId);
                        result.Add(new CoreAudioSessionData(
                            key,
                            processPath,
                            session.SimpleAudioVolume.Mute,
                            MapState(session.State)));
                    }
                }
            }
        }
        catch (Exception exception)
        {
            _log.Warn($"枚举音频会话失败：{exception.Message}");
        }

        return result;
    }

    public bool TrySetMute(SessionKey sessionKey, bool muted)
    {
        using var enumerator = new MMDeviceEnumerator();
        using var devices = enumerator.EnumerateAudioEndPoints(
            DataFlow.Render,
            DeviceState.Active);

        try
        {
            foreach (var device in devices)
            {
                using (device)
                {
                    var sessions = device.AudioSessionManager.Sessions;
                    for (var index = 0; index < sessions.Count; index++)
                    {
                        var session = sessions[index];
                        var key = new SessionKey(
                            session.GetSessionIdentifier,
                            session.GetSessionInstanceIdentifier,
                            session.GetProcessID);
                        if (key == sessionKey)
                        {
                            session.SimpleAudioVolume.Mute = muted;
                            return true;
                        }
                    }
                }
            }
        }
        catch (Exception exception)
        {
            _log.Warn($"修改音频会话失败：{exception.Message}");
        }

        return false;
    }

    private static SessionPlaybackState MapState(AudioSessionState state)
    {
        return state switch
        {
            AudioSessionState.Active => SessionPlaybackState.Active,
            AudioSessionState.Expired => SessionPlaybackState.Expired,
            _ => SessionPlaybackState.Inactive
        };
    }
}
```

Create `src/ChatDND.Core/Audio/WasapiAudioSessionProvider.cs`:

```csharp
using ChatDND.Core.Models;

namespace ChatDND.Core.Audio;

public sealed class WasapiAudioSessionProvider : IAudioSessionProvider
{
    private readonly ICoreAudioSessionSource _source;

    public WasapiAudioSessionProvider(ICoreAudioSessionSource source)
    {
        _source = source;
    }

    public IReadOnlyList<AudioSessionSnapshot> GetSessions()
    {
        return _source.Enumerate()
            .Select(CoreAudioSessionMapper.ToSnapshot)
            .ToArray();
    }
}
```

Create `src/ChatDND.Core/Audio/WasapiAudioSessionController.cs`:

```csharp
using ChatDND.Core.Models;

namespace ChatDND.Core.Audio;

public sealed class WasapiAudioSessionController : IAudioSessionController
{
    private readonly ICoreAudioSessionSource _source;

    public WasapiAudioSessionController(ICoreAudioSessionSource source)
    {
        _source = source;
    }

    public bool TrySetMute(SessionKey sessionKey, bool muted)
    {
        return _source.TrySetMute(sessionKey, muted);
    }
}
```

- [ ] **Step 5: Run the tests and verify they pass**

Run:

```powershell
dotnet test tests/ChatDND.Core.Tests/ChatDND.Core.Tests.csproj --filter "FullyQualifiedName~CoreAudioSessionMapper|FullyQualifiedName~ProcessPathResolver"
```

Expected: PASS. The adapter test is a mapping test; the live session behavior is covered by the manual integration test in Task 11.

- [ ] **Step 6: Commit**

Run:

```powershell
git add src/ChatDND.Core tests/ChatDND.Core.Tests
git commit -m "feat: add WASAPI session adapter"
```

---

### Task 7: Add Process Discovery and First-Run Candidate Rules

**Files:**
- Create: `src/ChatDND.Core/Models/CandidateApp.cs`
- Create: `src/ChatDND.Core/Services/ProcessDiscoveryService.cs`
- Create: `tests/ChatDND.Core.Tests/Services/ProcessDiscoveryServiceTests.cs`

**Interfaces:**
- Consumes: `IAudioSessionProvider`, `AudioSessionSnapshot`.
- Produces: `CandidateApp`, `ProcessDiscoveryService.Discover()`.

- [ ] **Step 1: Write the failing discovery test**

Create `tests/ChatDND.Core.Tests/Services/ProcessDiscoveryServiceTests.cs`:

```csharp
using ChatDND.Core.Models;
using ChatDND.Core.Services;
using ChatDND.Core.Tests.Services.Fakes;

namespace ChatDND.Core.Tests.Services;

public sealed class ProcessDiscoveryServiceTests
{
    [Fact]
    public void Discover_ReturnsDistinctApplicationsAndSkipsCurrentProcess()
    {
        var provider = new FakeAudioSessionProvider();
        provider.Sessions.AddRange([
            Session(@"C:\Program Files\Tencent\WeChat\WeChat.exe", 10),
            Session(@"c:\program files\tencent\wechat\wechat.exe", 11),
            Session(@"C:\Program Files\DingTalk\DingTalk.exe", 12),
            Session(@"C:\Tools\ChatDND.exe", (uint)Environment.ProcessId)
        ]);
        var service = new ProcessDiscoveryService(provider);

        var candidates = service.Discover();

        Assert.Equal(2, candidates.Count);
        Assert.Contains(candidates, item => item.DisplayName.Equals("WeChat", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(candidates, item => item.DisplayName.Equals("DingTalk", StringComparison.OrdinalIgnoreCase));
    }

    private static AudioSessionSnapshot Session(string path, uint pid)
    {
        return new AudioSessionSnapshot(
            new SessionKey($"session-{pid}", $"instance-{pid}", pid),
            path,
            false,
            SessionPlaybackState.Active);
    }
}
```

- [ ] **Step 2: Run the test and verify it fails**

Run:

```powershell
dotnet test tests/ChatDND.Core.Tests/ChatDND.Core.Tests.csproj --filter FullyQualifiedName~ProcessDiscoveryServiceTests
```

Expected: compile failure because discovery types do not exist.

- [ ] **Step 3: Implement candidate discovery**

Create `src/ChatDND.Core/Models/CandidateApp.cs`:

```csharp
namespace ChatDND.Core.Models;

public sealed record CandidateApp(string DisplayName, string ProcessPath);
```

Create `src/ChatDND.Core/Services/ProcessDiscoveryService.cs`:

```csharp
using ChatDND.Core.Audio;
using ChatDND.Core.Matching;
using ChatDND.Core.Models;

namespace ChatDND.Core.Services;

public sealed class ProcessDiscoveryService
{
    private readonly IAudioSessionProvider _provider;

    public ProcessDiscoveryService(IAudioSessionProvider provider)
    {
        _provider = provider;
    }

    public IReadOnlyList<CandidateApp> Discover()
    {
        var currentProcessId = (uint)Environment.ProcessId;
        var candidates = new Dictionary<string, CandidateApp>(StringComparer.OrdinalIgnoreCase);

        foreach (var session in _provider.GetSessions())
        {
            if (session.Key.ProcessId == currentProcessId
                || !ProcessPathNormalizer.TryNormalize(session.ProcessPath, out var path))
            {
                continue;
            }

            var name = Path.GetFileNameWithoutExtension(path);
            if (!candidates.ContainsKey(path))
            {
                candidates[path] = new CandidateApp(name, session.ProcessPath);
            }
        }

        return candidates.Values
            .OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
```

- [ ] **Step 4: Run the test and verify it passes**

Run:

```powershell
dotnet test tests/ChatDND.Core.Tests/ChatDND.Core.Tests.csproj --filter FullyQualifiedName~ProcessDiscoveryServiceTests
```

Expected: PASS.

- [ ] **Step 5: Commit**

Run:

```powershell
git add src/ChatDND.Core tests/ChatDND.Core.Tests
git commit -m "feat: discover first-run application candidates"
```

---

### Task 8: Add Local Paths, Logging, and a Rolling File

**Files:**
- Create: `src/ChatDND.Core/Services/AppPaths.cs`
- Create: `src/ChatDND.Core/Logging/RollingFileLog.cs`
- Create: `tests/ChatDND.Core.Tests/Services/AppPathsTests.cs`
- Create: `tests/ChatDND.Core.Tests/Logging/RollingFileLogTests.cs`

**Interfaces:**
- Consumes: `ILog` from Task 6.
- Produces: `AppPaths.DataDirectory`, `AppPaths.SettingsPath`, `AppPaths.RecoveryPath`, `RollingFileLog`.

- [ ] **Step 1: Write failing path and log tests**

Create `tests/ChatDND.Core.Tests/Services/AppPathsTests.cs`:

```csharp
using ChatDND.Core.Services;

namespace ChatDND.Core.Tests.Services;

public sealed class AppPathsTests
{
    [Fact]
    public void Paths_AreInsideLocalApplicationData()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        Assert.True(AppPaths.DataDirectory.StartsWith(local, StringComparison.OrdinalIgnoreCase));
        Assert.True(AppPaths.SettingsPath.EndsWith("settings.json", StringComparison.OrdinalIgnoreCase));
        Assert.True(AppPaths.RecoveryPath.EndsWith("recovery.json", StringComparison.OrdinalIgnoreCase));
    }
}
```

Create `tests/ChatDND.Core.Tests/Logging/RollingFileLogTests.cs`:

```csharp
using ChatDND.Core.Logging;

namespace ChatDND.Core.Tests.Logging;

public sealed class RollingFileLogTests
{
    [Fact]
    public void Error_WritesTimestampedChineseMessageToFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "chatdnd.log");
        var log = new RollingFileLog(path, maxBytes: 4096);

        log.Error("测试错误");

        var text = File.ReadAllText(path);
        Assert.Contains("测试错误", text);
        Assert.Contains("[ERROR]", text);
    }
}
```

- [ ] **Step 2: Run the tests and verify they fail**

Run:

```powershell
dotnet test tests/ChatDND.Core.Tests/ChatDND.Core.Tests.csproj --filter "FullyQualifiedName~AppPathsTests|FullyQualifiedName~RollingFileLogTests"
```

Expected: compile failure because path and logging types do not exist.

- [ ] **Step 3: Implement paths and rolling logs**

Create `src/ChatDND.Core/Services/AppPaths.cs`:

```csharp
namespace ChatDND.Core.Services;

public static class AppPaths
{
    public static string DataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ChatDND");

    public static string SettingsPath { get; } = Path.Combine(
        DataDirectory,
        "settings.json");

    public static string RecoveryPath { get; } = Path.Combine(
        DataDirectory,
        "recovery.json");

    public static string LogPath { get; } = Path.Combine(
        DataDirectory,
        "chatdnd.log");
}
```

Create `src/ChatDND.Core/Logging/RollingFileLog.cs`:

```csharp
namespace ChatDND.Core.Logging;

public sealed class RollingFileLog : ILog
{
    private readonly string _path;
    private readonly long _maxBytes;
    private readonly object _gate = new();

    public RollingFileLog(string path, long maxBytes = 1_048_576)
    {
        _path = path;
        _maxBytes = maxBytes;
    }

    public void Info(string message) => Write("INFO", message);

    public void Warn(string message) => Write("WARN", message);

    public void Error(string message) => Write("ERROR", message);

    private void Write(string level, string message)
    {
        lock (_gate)
        {
            var directory = Path.GetDirectoryName(_path)
                ?? throw new InvalidOperationException("Log path has no directory.");
            Directory.CreateDirectory(directory);
            if (File.Exists(_path) && new FileInfo(_path).Length >= _maxBytes)
            {
                File.Move(
                    _path,
                    $"{_path}.1",
                    overwrite: true);
            }

            File.AppendAllText(
                _path,
                $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}");
        }
    }
}
```

- [ ] **Step 4: Run the tests and verify they pass**

Run:

```powershell
dotnet test tests/ChatDND.Core.Tests/ChatDND.Core.Tests.csproj --filter "FullyQualifiedName~AppPathsTests|FullyQualifiedName~RollingFileLogTests"
```

Expected: PASS.

- [ ] **Step 5: Commit**

Run:

```powershell
git add src/ChatDND.Core tests/ChatDND.Core.Tests
git commit -m "feat: add local paths and rolling logs"
```

---

### Task 9: Build the Chinese WinForms Tray UI

**Files:**
- Create: `src/ChatDND.App/UiStrings.cs`
- Create: `src/ChatDND.App/AppController.cs`
- Create: `src/ChatDND.App/MainForm.cs`
- Create: `src/ChatDND.App/TrayApplicationContext.cs`
- Create: `src/ChatDND.App/SettingsForm.cs`
- Modify: `src/ChatDND.App/Program.cs`
- Create: `tests/ChatDND.Core.Tests/App/UiStringsTests.cs`

**Interfaces:**
- Consumes: `AppRuleStore`, `DndCoordinator`, `RecoveryService`, `ProcessDiscoveryService`, `ILog`.
- Produces: `ChatDND.App` executable with a Chinese main window, tray icon, and a single `AppController` that owns the coordinator.

- [ ] **Step 1: Write the failing UI string test**

Create `tests/ChatDND.Core.Tests/App/UiStringsTests.cs`:

```csharp
using ChatDND.App;

namespace ChatDND.Core.Tests.App;

public sealed class UiStringsTests
{
    [Fact]
    public void PrimaryLabels_AreChinese()
    {
        Assert.Equal("应用级免打扰", UiStrings.AppTitle);
        Assert.Equal("开启免打扰", UiStrings.EnableDnd);
        Assert.Equal("退出程序", UiStrings.ExitApplication);
        Assert.Equal("删除选中规则", UiStrings.RemoveSelectedRule);
        Assert.Equal("以管理员身份运行（提高兼容性，存在系统权限风险）", UiStrings.RunElevated);
    }
}
```

Add a project reference from `ChatDND.Core.Tests` to `ChatDND.App` in the test project:

```powershell
dotnet add tests/ChatDND.Core.Tests/ChatDND.Core.Tests.csproj reference src/ChatDND.App/ChatDND.App.csproj
```

- [ ] **Step 2: Run the test and verify it fails**

Run:

```powershell
dotnet test tests/ChatDND.Core.Tests/ChatDND.Core.Tests.csproj --filter FullyQualifiedName~UiStringsTests
```

Expected: compile failure because `UiStrings` does not exist.

- [ ] **Step 3: Add Chinese UI constants**

Create `src/ChatDND.App/UiStrings.cs`:

```csharp
namespace ChatDND.App;

public static class UiStrings
{
    public const string AppTitle = "应用级免打扰";
    public const string EnableDnd = "开启免打扰";
    public const string DisableDnd = "关闭免打扰";
    public const string AddCurrentApp = "添加当前应用";
    public const string AddExe = "手动选择程序";
    public const string RemoveSelectedRule = "删除选中规则";
    public const string RecommendedApps = "推荐聊天应用";
    public const string NoRules = "请先选择至少一个需要静音的应用。";
    public const string RunElevated = "以管理员身份运行（提高兼容性，存在系统权限风险）";
    public const string ExitApplication = "退出程序";
    public const string FirstRunTitle = "第一次使用 ChatDND";
    public const string FirstRunDescription = "请选择需要免打扰的应用，程序只会静音它们的 Windows 音频会话。";
    public const string ElevationRiskTitle = "管理员模式风险说明";
    public const string ElevationRiskBody =
        "管理员权限会扩大程序影响范围；如果程序存在漏洞或被替换，影响可能扩大到系统级操作。\n" +
        "Windows 会弹出 UAC 确认；通过任务计划自动以最高权限启动会降低安全边界。\n" +
        "管理员权限也不能保证控制所有受保护、独占模式或跨会话音频。\n" +
        "管理员模式仍然不会读取或修改联系人、聊天记录、应用配置或账号状态。\n" +
        "如果管理员凭据属于其他 Windows 账户，设置不会与普通用户配置共享。";
}
```

- [ ] **Step 4: Implement the tray lifecycle and form**

Create `src/ChatDND.App/AppController.cs`:

```csharp
using ChatDND.Core.Logging;
using ChatDND.Core.Models;
using ChatDND.Core.Services;

namespace ChatDND.App;

public sealed class AppController : IDisposable
{
    private readonly DndCoordinator _coordinator;
    private readonly RecoveryService _recovery;
    private readonly AppRuleStore _store;
    private readonly ProcessDiscoveryService _discovery;
    private readonly ILog _log;
    private readonly System.Windows.Forms.Timer _timer;
    private DndSettings _settings;

    public AppController(
        DndCoordinator coordinator,
        RecoveryService recovery,
        AppRuleStore store,
        ProcessDiscoveryService discovery,
        ILog log)
    {
        _coordinator = coordinator;
        _recovery = recovery;
        _store = store;
        _discovery = discovery;
        _log = log;
        _settings = store.Load();
        _timer = new System.Windows.Forms.Timer
        {
            Interval = _settings.ScanIntervalMs
        };
        _timer.Tick += (_, _) => _coordinator.Tick(_settings.Rules);
    }

    public bool IsEnabled => _coordinator.IsEnabled;

    public DndSettings Settings => _settings;

    public IReadOnlyList<CandidateApp> DiscoverCandidates() => _discovery.Discover();

    public void Start()
    {
        _log.Info("ChatDND 启动。");
        _recovery.Recover();
        if (_settings.AutoEnableOnLaunch && _settings.Rules.Count > 0)
        {
            Enable();
        }
    }

    public void Enable()
    {
        var result = _coordinator.Enable(_settings.Rules);
        if (result == DndEnableResult.NoRules)
        {
            throw new InvalidOperationException(UiStrings.NoRules);
        }

        _timer.Start();
    }

    public void Disable()
    {
        _timer.Stop();
        _coordinator.Disable();
    }

    public void PauseForElevation()
    {
        _timer.Stop();
        _store.Save(_settings);
    }

    public void ResumeAfterFailedElevation()
    {
        if (_coordinator.IsEnabled)
        {
            _timer.Start();
        }
    }

    public void SaveSettings(DndSettings settings)
    {
        _settings = settings;
        _store.Save(settings);
        _timer.Interval = settings.ScanIntervalMs;
    }

    public void AddRule(CandidateApp candidate)
    {
        AddRule(candidate.ProcessPath, candidate.DisplayName);
    }

    public void AddRule(string processPath, string displayName)
    {
        var normalized = ChatDND.Core.Matching.ProcessPathNormalizer.Normalize(processPath);
        if (_settings.Rules.Any(rule => rule.ExecutablePaths.Any(path =>
                ChatDND.Core.Matching.ProcessPathNormalizer.Normalize(path) == normalized)))
        {
            return;
        }

        var rule = new AppRule(
            Guid.NewGuid().ToString("N"),
            displayName,
            [processPath]);
        _settings = _settings with { Rules = [.. _settings.Rules, rule] };
        _store.Save(_settings);
        _log.Info($"已添加应用规则：{displayName}");
    }

    public void RemoveRule(AppRule rule)
    {
        _settings = _settings with
        {
            Rules = _settings.Rules
                .Where(item => item.Id != rule.Id)
                .ToArray()
        };
        _store.Save(_settings);
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
        Disable();
    }
}
```

Create `src/ChatDND.App/MainForm.cs`:

```csharp
using ChatDND.Core.Models;

namespace ChatDND.App;

public sealed class MainForm : Form
{
    private readonly AppController _controller;
    private readonly CheckBox _toggle;
    private readonly ListBox _rules;

    public event EventHandler? ElevationRequested;

    public MainForm(AppController controller, IReadOnlyList<CandidateApp> candidates)
    {
        _controller = controller;
        Text = UiStrings.AppTitle;
        StartPosition = FormStartPosition.CenterScreen;
        Width = 720;
        Height = 460;
        MinimumSize = new Size(620, 400);

        _toggle = new CheckBox
        {
            Text = controller.IsEnabled ? UiStrings.DisableDnd : UiStrings.EnableDnd,
            Checked = controller.IsEnabled,
            AutoSize = true
        };
        _rules = new ListBox
        {
            Dock = DockStyle.Fill,
            DisplayMember = nameof(AppRule.DisplayName)
        };
        RefreshRules();

        var addCurrent = new Button
        {
            Text = UiStrings.AddCurrentApp,
            AutoSize = true
        };
        addCurrent.Click += (_, _) =>
        {
            using var picker = new SettingsForm(candidates);
            if (picker.ShowDialog(this) == DialogResult.OK && picker.SelectedCandidate is not null)
            {
                _controller.AddRule(picker.SelectedCandidate);
                RefreshRules();
            }
        };

        var addExe = new Button
        {
            Text = UiStrings.AddExe,
            AutoSize = true
        };
        addExe.Click += (_, _) =>
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "可执行文件 (*.exe)|*.exe",
                CheckFileExists = true
            };
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _controller.AddRule(
                    dialog.FileName,
                    Path.GetFileNameWithoutExtension(dialog.FileName));
                RefreshRules();
            }
        };

        var elevate = new Button
        {
            Text = UiStrings.RunElevated,
            AutoSize = true
        };
        elevate.Click += (_, _) => ElevationRequested?.Invoke(this, EventArgs.Empty);

        var remove = new Button
        {
            Text = UiStrings.RemoveSelectedRule,
            AutoSize = true
        };
        remove.Click += (_, _) =>
        {
            if (_rules.SelectedItem is AppRule rule)
            {
                _controller.RemoveRule(rule);
                RefreshRules();
            }
        };

        _toggle.CheckedChanged += (_, _) =>
        {
            try
            {
                if (_toggle.Checked)
                {
                    _controller.Enable();
                    _toggle.Text = UiStrings.DisableDnd;
                }
                else
                {
                    _controller.Disable();
                    _toggle.Text = UiStrings.EnableDnd;
                }
            }
            catch (InvalidOperationException exception)
            {
                _toggle.Checked = false;
                MessageBox.Show(
                    exception.Message,
                    UiStrings.AppTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        };

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight
        };
        buttons.Controls.AddRange([addExe, addCurrent, remove, elevate]);
        Controls.Add(_rules);
        Controls.Add(buttons);
        Controls.Add(_toggle);
    }

    private void RefreshRules()
    {
        _rules.Items.Clear();
        _rules.Items.AddRange(_controller.Settings.Rules.ToArray());
    }
}
```

Create `src/ChatDND.App/SettingsForm.cs`:

```csharp
using ChatDND.Core.Models;

namespace ChatDND.App;

public sealed class SettingsForm : Form
{
    private readonly ListBox _candidates;

    public SettingsForm(IReadOnlyList<CandidateApp> candidates)
    {
        Text = UiStrings.FirstRunTitle;
        StartPosition = FormStartPosition.CenterParent;
        Width = 560;
        Height = 360;
        _candidates = new ListBox
        {
            Dock = DockStyle.Fill,
            DisplayMember = nameof(CandidateApp.DisplayName)
        };
        _candidates.Items.AddRange(candidates.ToArray());

        var description = new Label
        {
            Text = UiStrings.FirstRunDescription,
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(8)
        };
        var confirm = new Button
        {
            Text = "确定",
            DialogResult = DialogResult.OK,
            AutoSize = true
        };
        var cancel = new Button
        {
            Text = "取消",
            DialogResult = DialogResult.Cancel,
            AutoSize = true
        };
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft
        };
        buttons.Controls.AddRange([confirm, cancel]);
        Controls.Add(_candidates);
        Controls.Add(buttons);
        Controls.Add(description);
        AcceptButton = confirm;
        CancelButton = cancel;
    }

    public CandidateApp? SelectedCandidate => _candidates.SelectedItem as CandidateApp;
}
```

Create `src/ChatDND.App/TrayApplicationContext.cs`:

```csharp
using ChatDND.Core.Privileges;

namespace ChatDND.App;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _icon;
    private readonly AppController _controller;
    private readonly SingleInstanceGuard _singleInstance;
    private readonly IElevationLauncher _elevationLauncher;

    public TrayApplicationContext(
        AppController controller,
        SingleInstanceGuard singleInstance,
        IElevationLauncher elevationLauncher)
    {
        _controller = controller;
        _singleInstance = singleInstance;
        _elevationLauncher = elevationLauncher;
        _icon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = UiStrings.AppTitle,
            Visible = true
        };
        _icon.DoubleClick += (_, _) => ShowMainForm();
        _icon.ContextMenuStrip = BuildMenu();
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add(UiStrings.EnableDnd, null, (_, _) => Execute(_controller.Enable));
        menu.Items.Add(UiStrings.DisableDnd, null, (_, _) => Execute(_controller.Disable));
        menu.Items.Add(UiStrings.RunElevated, null, (_, _) => Elevate());
        menu.Items.Add(UiStrings.ExitApplication, null, (_, _) =>
        {
            _controller.Dispose();
            _icon.Visible = false;
            ExitThread();
        });
        return menu;
    }

    private void Execute(Action action)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException exception)
        {
            MessageBox.Show(
                exception.Message,
                UiStrings.AppTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }

    private void Elevate()
    {
        var result = MessageBox.Show(
            UiStrings.ElevationRiskBody,
            UiStrings.ElevationRiskTitle,
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Warning);
        if (result != DialogResult.OK)
        {
            return;
        }

        _controller.PauseForElevation();
        _singleInstance.Release();
        if (_elevationLauncher.TryRestartElevated())
        {
            _icon.Visible = false;
            ExitThread();
            return;
        }

        _singleInstance.TryAcquire();
        _controller.ResumeAfterFailedElevation();
        MessageBox.Show(
            "未能启动管理员模式，程序将继续使用普通权限。",
            UiStrings.AppTitle,
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void ShowMainForm()
    {
        var form = new MainForm(_controller, _controller.DiscoverCandidates());
        form.ElevationRequested += (_, _) => Elevate();
        form.Show();
    }
}
```

Modify `src/ChatDND.App/Program.cs`:

```csharp
using ChatDND.Core.Audio;
using ChatDND.Core.Logging;
using ChatDND.Core.Privileges;
using ChatDND.Core.Services;

namespace ChatDND.App;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        using var singleInstance = new SingleInstanceGuard();
        if (!singleInstance.TryAcquire())
        {
            return;
        }

        var log = new RollingFileLog(AppPaths.LogPath);
        var source = new NaudioCoreAudioSessionSource(log);
        var provider = new WasapiAudioSessionProvider(source);
        var controller = new WasapiAudioSessionController(source);
        var journal = new JsonRecoveryJournal(AppPaths.RecoveryPath);
        var store = new AppRuleStore(AppPaths.SettingsPath);
        var coordinator = new DndCoordinator(provider, controller, journal);
        var recovery = new RecoveryService(provider, controller, journal);
        var discovery = new ProcessDiscoveryService(provider);
        var appController = new AppController(coordinator, recovery, store, discovery, log);
        if (args.Contains("--resume-dnd", StringComparer.OrdinalIgnoreCase))
        {
            appController.Enable();
        }
        else
        {
            appController.Start();
        }

        var elevationLauncher = new ElevationLauncher(
            Application.ExecutablePath,
            new SystemProcessLauncher());
        Application.Run(new TrayApplicationContext(
            appController,
            singleInstance,
            elevationLauncher));
    }
}
```

- [ ] **Step 5: Run tests and build the application**

Run:

```powershell
dotnet test ChatDND.sln
dotnet build ChatDND.sln
```

Expected: tests pass and the App project builds. The UI is then manually checked in Task 11 because WinForms behavior is not unit-testable without a desktop session.

- [ ] **Step 6: Commit**

Run:

```powershell
git add src/ChatDND.App src/ChatDND.Core tests/ChatDND.Core.Tests
git commit -m "feat: add Chinese tray application"
```

---

### Task 10: Add Optional Admin Mode, UAC Confirmation, and Single-Instance Handoff

**Files:**
- Create: `src/ChatDND.Core/Privileges/IPrivilegeService.cs`
- Create: `src/ChatDND.Core/Privileges/PrivilegeService.cs`
- Create: `src/ChatDND.Core/Privileges/IElevationLauncher.cs`
- Create: `src/ChatDND.Core/Privileges/ElevationLauncher.cs`
- Create: `src/ChatDND.Core/Privileges/SingleInstanceGuard.cs`
- Create: `tests/ChatDND.Core.Tests/Privileges/PrivilegeServiceTests.cs`
- Create: `tests/ChatDND.Core.Tests/Privileges/ElevationLauncherTests.cs`
- Modify: `src/ChatDND.App/Program.cs`
- Modify: `src/ChatDND.App/TrayApplicationContext.cs`

**Interfaces:**
- Consumes: `UiStrings.ElevationRiskTitle`, `UiStrings.ElevationRiskBody`.
- Produces: `IPrivilegeService.IsElevated`, `IElevationLauncher.TryRestartElevated()`, `SingleInstanceGuard.TryAcquire()`.

- [ ] **Step 1: Write failing privilege tests**

Create `tests/ChatDND.Core.Tests/Privileges/PrivilegeServiceTests.cs`:

```csharp
using ChatDND.Core.Privileges;

namespace ChatDND.Core.Tests.Privileges;

public sealed class PrivilegeServiceTests
{
    [Fact]
    public void IsElevated_UsesInjectedIdentity()
    {
        var service = new PrivilegeService(() => true);

        Assert.True(service.IsElevated);
    }
}
```

Create `tests/ChatDND.Core.Tests/Privileges/ElevationLauncherTests.cs`:

```csharp
using ChatDND.Core.Privileges;

namespace ChatDND.Core.Tests.Privileges;

public sealed class ElevationLauncherTests
{
    [Fact]
    public void TryRestartElevated_UsesRunasVerbAndElevatedArgument()
    {
        var launcher = new FakeProcessLauncher();
        var service = new ElevationLauncher(
            @"C:\Apps\ChatDND.exe",
            launcher);

        var result = service.TryRestartElevated();

        Assert.True(result);
        Assert.Equal("runas", launcher.Verb);
        Assert.Contains("--elevated", launcher.Arguments);
    }

    private sealed class FakeProcessLauncher : IProcessLauncher
    {
        public string? Verb { get; private set; }

        public string? Arguments { get; private set; }

        public bool TryStart(string fileName, string arguments, string verb)
        {
            Arguments = arguments;
            Verb = verb;
            return true;
        }
    }
}
```

- [ ] **Step 2: Run the tests and verify they fail**

Run:

```powershell
dotnet test tests/ChatDND.Core.Tests/ChatDND.Core.Tests.csproj --filter FullyQualifiedName~Privileges
```

Expected: compile failure because privilege types do not exist.

- [ ] **Step 3: Implement privilege and elevation abstractions**

Create `src/ChatDND.Core/Privileges/IPrivilegeService.cs`:

```csharp
namespace ChatDND.Core.Privileges;

public interface IPrivilegeService
{
    bool IsElevated { get; }
}
```

Create `src/ChatDND.Core/Privileges/PrivilegeService.cs`:

```csharp
namespace ChatDND.Core.Privileges;

public sealed class PrivilegeService : IPrivilegeService
{
    private readonly Func<bool> _isElevated;

    public PrivilegeService(Func<bool>? isElevated = null)
    {
        _isElevated = isElevated ?? Detect;
    }

    public bool IsElevated => _isElevated();

    private static bool Detect()
    {
        using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
        var principal = new System.Security.Principal.WindowsPrincipal(identity);
        return principal.IsInRole(
            System.Security.Principal.WindowsBuiltInRole.Administrator);
    }
}
```

Create `src/ChatDND.Core/Privileges/IElevationLauncher.cs`:

```csharp
namespace ChatDND.Core.Privileges;

public interface IElevationLauncher
{
    bool TryRestartElevated();
}

public interface IProcessLauncher
{
    bool TryStart(string fileName, string arguments, string verb);
}
```

Create `src/ChatDND.Core/Privileges/ElevationLauncher.cs`:

```csharp
using System.Diagnostics;

namespace ChatDND.Core.Privileges;

public sealed class ElevationLauncher : IElevationLauncher
{
    private readonly string _executablePath;
    private readonly IProcessLauncher _launcher;

    public ElevationLauncher(string executablePath, IProcessLauncher launcher)
    {
        _executablePath = executablePath;
        _launcher = launcher;
    }

    public bool TryRestartElevated()
    {
        return _launcher.TryStart(
            _executablePath,
            "--elevated --resume-dnd",
            "runas");
    }
}

public sealed class SystemProcessLauncher : IProcessLauncher
{
    public bool TryStart(string fileName, string arguments, string verb)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                Verb = verb,
                UseShellExecute = true
            }
        };

        try
        {
            return process.Start();
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }
}
```

Create `src/ChatDND.Core/Privileges/SingleInstanceGuard.cs`:

```csharp
namespace ChatDND.Core.Privileges;

public sealed class SingleInstanceGuard : IDisposable
{
    private readonly Mutex _mutex;
    private bool _isOwner;

    public SingleInstanceGuard(string name = @"Local\ChatDND.SingleInstance")
    {
        _mutex = new Mutex(initiallyOwned: false, name);
    }

    public bool IsOwner => _isOwner;

    public bool TryAcquire()
    {
        if (_isOwner)
        {
            return true;
        }

        try
        {
            _isOwner = _mutex.WaitOne(0);
        }
        catch (AbandonedMutexException)
        {
            _isOwner = true;
        }

        return _isOwner;
    }

    public void Release()
    {
        if (!_isOwner)
        {
            return;
        }

        _mutex.ReleaseMutex();
        _isOwner = false;
    }

    public void Dispose()
    {
        Release();
        _mutex.Dispose();
    }
}
```

- [ ] **Step 4: Wire the risk dialog and UAC path into the app**

In `Program.cs`, add before creating the controller:

```csharp
using var singleInstance = new SingleInstanceGuard();
if (!singleInstance.IsOwner)
{
    return;
}

```

Add an elevation menu item in `TrayApplicationContext`:

```csharp
menu.Items.Add(UiStrings.RunElevated, null, (_, _) =>
{
    var result = MessageBox.Show(
        UiStrings.ElevationRiskBody,
        UiStrings.ElevationRiskTitle,
        MessageBoxButtons.OKCancel,
        MessageBoxIcon.Warning);
    if (result != DialogResult.OK)
    {
        return;
    }

    var launcher = new ElevationLauncher(
        Application.ExecutablePath,
        new SystemProcessLauncher());
    if (!launcher.TryRestartElevated())
    {
        MessageBox.Show(
            "未能启动管理员模式，程序将继续使用普通权限。",
            UiStrings.AppTitle,
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }
});
```

The handoff sequence is explicit: `PauseForElevation` stops the ordinary timer and saves settings, `Release` drops the mutex, the elevated process acquires the mutex and starts with `--elevated --resume-dnd`, and the ordinary process exits without calling `Disable` so the elevated instance owns restoration. If UAC or process startup fails, the ordinary instance calls `TryAcquire` and `ResumeAfterFailedElevation`.

- [ ] **Step 5: Run tests and manual UAC check**

Run:

```powershell
dotnet test ChatDND.sln
dotnet run --project src/ChatDND.App/ChatDND.App.csproj
```

Manual check:

1. Start normally and confirm no UAC dialog appears.
2. Click the admin mode menu item and confirm the Chinese risk dialog appears.
3. Cancel the risk dialog and confirm the app continues normally.
4. Approve UAC and confirm the elevated process starts.
5. Cancel UAC at the Windows prompt and confirm the ordinary instance continues.

- [ ] **Step 6: Commit**

Run:

```powershell
git add src/ChatDND.Core src/ChatDND.App tests/ChatDND.Core.Tests
git commit -m "feat: add optional admin mode handoff"
```

---

### Task 11: Final Integration, Memory Measurement, and Release Verification

**Files:**
- Create: `tools/Measure-ChatDND.ps1`
- Create: `docs/verification/windows-chat-dnd-acceptance.md`
- Modify: `src/ChatDND.App/ChatDND.App.csproj` for release metadata.

**Interfaces:**
- Consumes: all prior tasks.
- Produces: a documented release verification process and a repeatable memory measurement command.

- [ ] **Step 1: Add the memory measurement script**

Create `tools/Measure-ChatDND.ps1`:

```powershell
param(
    [string]$ProcessName = "ChatDND",
    [int]$Samples = 60
)

$process = Get-Process -Name $ProcessName -ErrorAction Stop
$values = for ($i = 0; $i -lt $Samples; $i++) {
    $process.Refresh()
    [pscustomobject]@{
        Time = Get-Date
        WorkingSetMB = [math]::Round($process.WorkingSet64 / 1MB, 2)
    }
    Start-Sleep -Seconds 1
}

$summary = $values | Measure-Object WorkingSetMB -Average -Maximum -Minimum
$values | Format-Table -AutoSize
$summary | Format-List
```

- [ ] **Step 2: Add the manual acceptance checklist**

Create `docs/verification/windows-chat-dnd-acceptance.md` with:

```markdown
# ChatDND Windows Acceptance Checklist

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
- [ ] Cancelling UAC leaves the ordinary instance running.
- [ ] The UI, tray menu, dialogs, and errors are Chinese; English technical terms have Chinese explanations.
- [ ] `tools/Measure-ChatDND.ps1` reports idle working set and long-run memory values.
```

- [ ] **Step 3: Publish a portable release**

Add these properties to the first `PropertyGroup` in `src/ChatDND.App/ChatDND.App.csproj`:

```xml
<AssemblyTitle>ChatDND</AssemblyTitle>
<Product>ChatDND</Product>
<Description>Windows 应用级免打扰工具</Description>
<Company>ChatDND</Company>
<Version>0.1.0</Version>
```

Run:

```powershell
dotnet publish src/ChatDND.App/ChatDND.App.csproj `
  -c Release `
  -r win-x64 `
  --self-contained false `
  -p:PublishSingleFile=false
```

Expected: publish succeeds under `src/ChatDND.App/bin/Release/net10.0-windows/win-x64/publish/`.

- [ ] **Step 4: Run the complete test suite**

Run:

```powershell
dotnet test ChatDND.sln -c Release
```

Expected: all tests pass.

- [ ] **Step 5: Run manual acceptance**

Run the published `ChatDND.App.exe`, execute every item in `docs/verification/windows-chat-dnd-acceptance.md`, and record the results in that file. Test with the actual WeChat, QQ, and DingTalk instances available on the machine.

- [ ] **Step 6: Commit**

Run:

```powershell
git add tools docs/verification src/ChatDND.App
git commit -m "test: add acceptance and memory verification"
```

---

## Plan Self-Review

### Spec Coverage

| Spec requirement | Implementing task |
|---|---|
| Independent Windows tray application | Tasks 1 and 9 |
| Manual DND toggle | Tasks 4, 9, and 10 |
| Default auto-enable after first configuration | Tasks 3, 8, and 9 |
| All active render endpoints | Task 6 |
| Multiple instances and later-created sessions | Tasks 4 and 6 |
| No modification of application internals | Global constraints; Tasks 3 and 6 |
| Chinese interface | Tasks 8 and 9 |
| Optional admin mode and risk prompt | Task 10 |
| Original mute-state restoration | Tasks 4 and 5 |
| Crash recovery | Task 5 |
| Low memory target and measurement | Tasks 1, 6, 8, and 11 |
| First-run app discovery and manual EXE selection | Task 7 and Task 9 |
| No rules must not fake enabled state | Tasks 4 and 9 |
| Shared helper process safety | Tasks 2 and 6 |
| Device switching and disconnect handling | Task 6 and Task 11 |

### Placeholder Scan

The plan contains no `TBD`, `TODO`, "implement later", or "add appropriate error handling" steps. Each implementation task names files, interfaces, tests, commands, and expected results.

### Type Consistency

The plan consistently uses `AppRule`, `SessionKey`, `AudioSessionSnapshot`, `CoreAudioSessionData`, `IAudioSessionProvider`, `IAudioSessionController`, `ICoreAudioSessionSource`, `DndCoordinator`, `RecoveryRecord`, `JsonRecoveryJournal`, `RecoveryService`, `ProcessDiscoveryService`, `DndSettings`, `AppRuleStore`, `IPrivilegeService`, and `ElevationLauncher`.

### Review Focus Ownership

- No audio session yet: `DndCoordinatorTests.Tick_MutesSessionAppearingAfterEnable` and manual acceptance.
- Two instances with different PIDs: `DndCoordinatorTests.Enable_MutesEveryMatchingInstance`.
- Shared helper process: `AppRuleMatcherTests` plus the explicit-path global constraint; unlisted helpers never match.
- UAC cancellation: `ElevationLauncherTests` plus manual UAC check in Task 10.
- Audio device switch: manual endpoint check in Task 11; the adapter enumerates all active render endpoints.
- Force-close recovery: `RecoveryServiceTests` and the force-close acceptance item.
