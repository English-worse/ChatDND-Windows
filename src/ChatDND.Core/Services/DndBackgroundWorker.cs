using System.Collections.Concurrent;
using ChatDND.Core.Logging;
using ChatDND.Core.Models;

namespace ChatDND.Core.Services;

public sealed class DndBackgroundWorker : IDisposable
{
    private readonly DndCoordinator _coordinator;
    private readonly RecoveryService _recovery;
    private readonly ILog _log;
    private readonly BlockingCollection<Action> _commands = new();
    private readonly Thread _thread;
    private int _enabled;
    private int _scanIntervalMs;
    private IReadOnlyList<AppRule> _rules = [];
    private bool _disposed;

    public DndBackgroundWorker(
        DndCoordinator coordinator,
        RecoveryService recovery,
        ILog log,
        int scanIntervalMs = 500)
    {
        _coordinator = coordinator;
        _recovery = recovery;
        _log = log;
        _scanIntervalMs = Math.Clamp(scanIntervalMs, 100, 5000);
        _thread = new Thread(Run)
        {
            IsBackground = true,
            Name = "ChatDND.AudioScan"
        };
        _thread.SetApartmentState(ApartmentState.MTA);
    }

    public event EventHandler? StateChanged;

    public bool IsEnabled => Volatile.Read(ref _enabled) == 1;

    public void Start()
    {
        if (_thread.IsAlive)
        {
            return;
        }

        _thread.Start();
    }

    public void Recover()
    {
        Post(() => _recovery.Recover());
    }

    public void Enable(IReadOnlyList<AppRule> rules)
    {
        var snapshot = rules.ToArray();
        Post(() =>
        {
            _rules = snapshot;
            if (!HasEnabledRules(snapshot))
            {
                SetEnabled(false);
                return;
            }

            var result = _coordinator.Enable(snapshot);
            SetEnabled(result == DndEnableResult.Enabled);
        });
    }

    public void Disable()
    {
        Post(() =>
        {
            _coordinator.Disable();
            SetEnabled(false);
        });
    }

    public void UpdateRules(
        IReadOnlyList<AppRule> rules,
        int scanIntervalMs)
    {
        var snapshot = rules.ToArray();
        var interval = Math.Clamp(scanIntervalMs, 100, 5000);
        Post(() =>
        {
            _rules = snapshot;
            _scanIntervalMs = interval;
            if (IsEnabled && !HasEnabledRules(snapshot))
            {
                _coordinator.Disable();
                SetEnabled(false);
            }
        });
    }

    public bool StopAndDisable()
    {
        if (!_thread.IsAlive)
        {
            var wasEnabled = IsEnabled;
            _coordinator.Disable();
            SetEnabled(false);
            return wasEnabled;
        }

        return ExecuteSynchronously(() =>
        {
            var wasEnabled = IsEnabled;
            _coordinator.Disable();
            SetEnabled(false);
            return wasEnabled;
        });
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        StopAndDisable();
        _commands.CompleteAdding();
        if (_thread.IsAlive)
        {
            _thread.Join(TimeSpan.FromSeconds(5));
        }

        _commands.Dispose();
        _disposed = true;
    }

    private void Run()
    {
        while (!_commands.IsAddingCompleted || _commands.Count > 0)
        {
            try
            {
                if (_commands.TryTake(
                    out var command,
                    IsEnabled ? _scanIntervalMs : Timeout.Infinite))
                {
                    command();
                }
                else
                {
                    Tick();
                }
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception exception)
            {
                _log.Error($"后台扫描失败：{exception.Message}");
                TryDisableAfterFailure();
            }
        }
    }

    private void Tick()
    {
        if (!IsEnabled)
        {
            return;
        }

        if (!HasEnabledRules(_rules))
        {
            _coordinator.Disable();
            SetEnabled(false);
            return;
        }

        _coordinator.Tick(_rules);
    }

    private void TryDisableAfterFailure()
    {
        try
        {
            _coordinator.Disable();
        }
        catch (Exception exception)
        {
            _log.Error($"关闭免打扰失败：{exception.Message}");
        }

        SetEnabled(false);
    }

    private void Post(Action command)
    {
        if (_disposed || _commands.IsAddingCompleted)
        {
            return;
        }

        try
        {
            _commands.Add(command);
        }
        catch (InvalidOperationException)
        {
        }
    }

    private T ExecuteSynchronously<T>(Func<T> command)
    {
        var completion = new TaskCompletionSource<T>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        Post(() =>
        {
            try
            {
                completion.SetResult(command());
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        });

        return completion.Task.GetAwaiter().GetResult();
    }

    private void SetEnabled(bool enabled)
    {
        var value = enabled ? 1 : 0;
        if (Interlocked.Exchange(ref _enabled, value) != value)
        {
            try
            {
                StateChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception exception)
            {
                _log.Error($"通知界面状态失败：{exception.Message}");
            }
        }
    }

    private static bool HasEnabledRules(IReadOnlyList<AppRule> rules)
    {
        return rules.Count > 0 && rules.Any(rule => rule.Enabled);
    }
}
