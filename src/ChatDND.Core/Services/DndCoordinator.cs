using ChatDND.Core.Audio;
using ChatDND.Core.Logging;
using ChatDND.Core.Matching;
using ChatDND.Core.Models;

namespace ChatDND.Core.Services;

public sealed class DndCoordinator
{
    private static readonly TimeSpan JournalRetryDelay = TimeSpan.FromSeconds(5);

    private readonly IAudioSessionProvider _provider;
    private readonly IAudioSessionController _controller;
    private readonly IRecoveryJournal _journal;
    private readonly ILog _log;
    private readonly Func<DateTimeOffset> _utcNow;
    private readonly Dictionary<SessionKey, RecoveryRecord> _managed = [];
    private bool _journalDirty;
    private DateTimeOffset _nextJournalRetryAt = DateTimeOffset.MinValue;

    public DndCoordinator(
        IAudioSessionProvider provider,
        IAudioSessionController controller,
        IRecoveryJournal journal,
        ILog? log = null,
        Func<DateTimeOffset>? utcNow = null)
    {
        _provider = provider;
        _controller = controller;
        _journal = journal;
        _log = log ?? NullLog.Instance;
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
    }

    public bool IsEnabled { get; private set; }

    public DndEnableResult Enable(IReadOnlyList<AppRule> rules)
    {
        if (IsEnabled)
        {
            return DndEnableResult.Enabled;
        }

        if (!HasEnabledRules(rules))
        {
            return DndEnableResult.NoRules;
        }

        IsEnabled = true;
        ApplyRules(rules);
        return DndEnableResult.Enabled;
    }

    public void Tick(IReadOnlyList<AppRule> rules)
    {
        if (!IsEnabled)
        {
            return;
        }

        if (!HasEnabledRules(rules))
        {
            Disable();
            return;
        }

        ApplyRules(rules);
    }

    public void Disable()
    {
        if (!IsEnabled)
        {
            return;
        }

        IsEnabled = false;
        var scan = _provider.Scan();
        var liveKeys = scan.Sessions
            .Select(session => session.Key)
            .ToHashSet();
        var remainingRecords = new List<RecoveryRecord>();

        foreach (var record in _managed.Values)
        {
            if (!record.MutedByTool || record.OriginalMute)
            {
                continue;
            }

            if (scan.IsComplete && !liveKeys.Contains(record.Key))
            {
                continue;
            }

            if (liveKeys.Contains(record.Key)
                && _controller.TrySetMute(record.Key, muted: false))
            {
                continue;
            }

            remainingRecords.Add(record);
        }

        _managed.Clear();
        PersistRecords(remainingRecords, force: true);
    }

    private void ApplyRules(IReadOnlyList<AppRule> rules)
    {
        var scan = _provider.Scan();
        var sessions = scan.Sessions
            .GroupBy(item => item.Key)
            .Select(group => group.First())
            .ToArray();
        var liveKeys = sessions.Select(item => item.Key).ToHashSet();
        var matchingKeys = new HashSet<SessionKey>();
        var changed = false;

        foreach (var session in sessions)
        {
            if (AppRuleMatcher.Match(session.ProcessPath, rules) is null)
            {
                continue;
            }

            matchingKeys.Add(session.Key);
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
                    changed = true;
                }

                continue;
            }

            if (record.MutedByTool && !session.IsMuted)
            {
                _controller.TrySetMute(session.Key, muted: true);
            }
        }

        if (scan.IsComplete)
        {
            foreach (var key in _managed.Keys
                         .Where(key => !liveKeys.Contains(key) || !matchingKeys.Contains(key))
                         .ToArray())
            {
                var record = _managed[key];
                if (liveKeys.Contains(key)
                    && record.MutedByTool
                    && !record.OriginalMute)
                {
                    if (!_controller.TrySetMute(key, muted: false))
                    {
                        continue;
                    }
                }

                _managed.Remove(key);
                changed = true;
            }
        }
        else
        {
            _log.Warn(
                $"音频会话枚举不完整，已跳过会话清理。{scan.ErrorMessage}");
        }

        if (changed)
        {
            _journalDirty = true;
        }

        PersistRecords(GetPersistableRecords(), force: false);
    }

    private void PersistRecords(
        IReadOnlyCollection<RecoveryRecord> records,
        bool force)
    {
        if (!force && !_journalDirty)
        {
            return;
        }

        if (!force && _utcNow() < _nextJournalRetryAt)
        {
            return;
        }

        var saved = records.Count == 0
            ? _journal.TryClear()
            : _journal.TrySave(records);
        if (saved)
        {
            _journalDirty = false;
            _nextJournalRetryAt = DateTimeOffset.MinValue;
            return;
        }

        _journalDirty = true;
        _nextJournalRetryAt = _utcNow().Add(JournalRetryDelay);
        _log.Error("恢复日志写入失败，稍后重试。");
    }

    private RecoveryRecord[] GetPersistableRecords()
    {
        return _managed.Values
            .Where(record => record.MutedByTool && !record.OriginalMute)
            .ToArray();
    }

    private static bool HasEnabledRules(IReadOnlyList<AppRule> rules)
    {
        return rules.Count > 0 && rules.Any(rule => rule.Enabled);
    }
}
