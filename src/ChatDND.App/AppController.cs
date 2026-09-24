using ChatDND.Core.Logging;
using ChatDND.Core.Matching;
using ChatDND.Core.Models;
using ChatDND.Core.Services;

namespace ChatDND.App;

public sealed class AppController : IDisposable
{
    private readonly AppRuleStore _store;
    private readonly ProcessDiscoveryService _discovery;
    private readonly IAdminStartupService _adminStartup;
    private readonly ILog _log;
    private readonly DndBackgroundWorker _worker;
    private DndSettings _settings;

    public AppController(
        DndCoordinator coordinator,
        RecoveryService recovery,
        AppRuleStore store,
        ProcessDiscoveryService discovery,
        IAdminStartupService adminStartup,
        ILog log)
    {
        _store = store;
        _discovery = discovery;
        _adminStartup = adminStartup;
        _log = log;
        _settings = store.Load();
        _worker = new DndBackgroundWorker(
            coordinator,
            recovery,
            log,
            _settings.ScanIntervalMs);
        _worker.StateChanged += (_, _) => StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? StateChanged;

    public bool IsEnabled => _worker.IsEnabled;

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
        _worker.Start();
        _worker.Recover();
        if (autoEnable
            && _settings.AutoEnableOnLaunch
            && _settings.Rules.Any(rule => rule.Enabled))
        {
            Enable();
        }
    }

    public void Enable()
    {
        if (!_settings.Rules.Any(rule => rule.Enabled))
        {
            throw new InvalidOperationException(UiStrings.NoRules);
        }

        _worker.Enable(_settings.Rules);
    }

    public void Disable()
    {
        _worker.Disable();
    }

    public bool PrepareForElevation()
    {
        var wasEnabled = _worker.StopAndDisable();
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
        _worker.UpdateRules(settings.Rules, settings.ScanIntervalMs);
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

        if (!_settings.Rules.Any(item => item.Enabled) && _worker.IsEnabled)
        {
            Disable();
        }
    }

    public void Dispose()
    {
        _worker.Dispose();
    }
}
