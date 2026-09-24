using ChatDND.Core.Logging;
using ChatDND.Core.Matching;
using ChatDND.Core.Models;
using ChatDND.Core.Services;

namespace ChatDND.App;

public sealed class AppController : IDisposable
{
    private readonly DndCoordinator _coordinator;
    private readonly RecoveryService _recovery;
    private readonly AppRuleStore _store;
    private readonly ProcessDiscoveryService _discovery;
    private readonly IAdminStartupService _adminStartup;
    private readonly ILog _log;
    private readonly System.Windows.Forms.Timer _timer;
    private DndSettings _settings;

    public AppController(
        DndCoordinator coordinator,
        RecoveryService recovery,
        AppRuleStore store,
        ProcessDiscoveryService discovery,
        IAdminStartupService adminStartup,
        ILog log)
    {
        _coordinator = coordinator;
        _recovery = recovery;
        _store = store;
        _discovery = discovery;
        _adminStartup = adminStartup;
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

    public IReadOnlyList<CandidateApp> DiscoverCandidates()
    {
        return _discovery.Discover();
    }

    public void Start()
    {
        Start(autoEnable: true);
    }

    public void Start(bool autoEnable)
    {
        _log.Info("ChatDND 启动。");
        _recovery.Recover();
        if (autoEnable
            && _settings.AutoEnableOnLaunch
            && _settings.Rules.Any(rule => rule.Enabled))
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

    public bool PrepareForElevation()
    {
        _timer.Stop();
        var wasEnabled = _coordinator.IsEnabled;
        if (wasEnabled)
        {
            _coordinator.Disable();
        }

        _store.Save(_settings);
        return wasEnabled;
    }

    public void ResumeAfterFailedElevation(bool wasEnabled)
    {
        if (wasEnabled)
        {
            Enable();
        }
    }

    public void SaveSettings(DndSettings settings)
    {
        _settings = settings;
        _store.Save(settings);
        _timer.Interval = settings.ScanIntervalMs;
    }

    public void SaveGeneralSettings(
        bool autoEnableOnLaunch,
        bool minimizeToTrayOnStartup,
        bool runAsAdministratorAtStartup,
        int scanIntervalMs)
    {
        if (runAsAdministratorAtStartup != _settings.RunAsAdministratorAtStartup)
        {
            var updated = runAsAdministratorAtStartup
                ? _adminStartup.TryEnable()
                : _adminStartup.TryDisable();
            if (!updated)
            {
                throw new InvalidOperationException(UiStrings.AdminStartupFailed);
            }
        }

        SaveSettings(DndSettingsUpdater.UpdateGeneralSettings(
            _settings,
            autoEnableOnLaunch,
            minimizeToTrayOnStartup,
            runAsAdministratorAtStartup,
            scanIntervalMs));
    }

    public void AddRule(CandidateApp candidate)
    {
        AddRule(candidate.ProcessPath, candidate.DisplayName);
    }

    public void AddRule(string processPath, string displayName)
    {
        var normalized = ProcessPathNormalizer.Normalize(processPath);
        if (_settings.Rules.Any(rule => rule.ExecutablePaths.Any(
                path => ProcessPathNormalizer.TryNormalize(path, out var rulePath)
                    && rulePath.Equals(normalized, StringComparison.OrdinalIgnoreCase))))
        {
            return;
        }

        var rule = new AppRule(
            Guid.NewGuid().ToString("N"),
            displayName,
            [processPath]);
        SaveSettings(_settings with { Rules = [.. _settings.Rules, rule] });
        _log.Info($"已添加应用规则：{displayName}");
    }

    public void RemoveRule(AppRule rule)
    {
        SaveSettings(_settings with
        {
            Rules = _settings.Rules
                .Where(item => item.Id != rule.Id)
                .ToArray()
        });

        if (!_settings.Rules.Any(item => item.Enabled) && _coordinator.IsEnabled)
        {
            Disable();
        }
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
        if (_coordinator.IsEnabled)
        {
            _coordinator.Disable();
        }
    }
}
