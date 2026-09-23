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
        var sessions = GetSessionsByKey();
        var remainingRecords = new List<RecoveryRecord>();
        foreach (var record in _managed.Values)
        {
            if (sessions.ContainsKey(record.Key)
                && record.MutedByTool
                && !record.OriginalMute
                && !_controller.TrySetMute(record.Key, muted: false))
            {
                remainingRecords.Add(record);
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
        var sessions = GetSessionsByKey().Values.ToArray();
        var liveKeys = sessions.Select(item => item.Key).ToHashSet();
        var matchingKeys = new HashSet<SessionKey>();

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
                }

                continue;
            }

            if (record.MutedByTool && !session.IsMuted)
            {
                _controller.TrySetMute(session.Key, muted: true);
            }
        }

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
        }

        _journal.Save(_managed.Values.ToArray());
    }

    private Dictionary<SessionKey, AudioSessionSnapshot> GetSessionsByKey()
    {
        return _provider.GetSessions()
            .GroupBy(item => item.Key)
            .ToDictionary(group => group.Key, group => group.First());
    }
}
