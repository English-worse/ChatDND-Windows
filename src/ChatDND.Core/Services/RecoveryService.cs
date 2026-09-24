using ChatDND.Core.Audio;
using ChatDND.Core.Logging;

namespace ChatDND.Core.Services;

public sealed class RecoveryService
{
    private readonly IAudioSessionProvider _provider;
    private readonly IAudioSessionController _controller;
    private readonly IRecoveryJournal _journal;
    private readonly ILog _log;

    public RecoveryService(
        IAudioSessionProvider provider,
        IAudioSessionController controller,
        IRecoveryJournal journal,
        ILog log)
    {
        _provider = provider;
        _controller = controller;
        _journal = journal;
        _log = log;
    }

    public void Recover()
    {
        var records = _journal.Load();
        if (records.Count == 0)
        {
            return;
        }

        var scan = _provider.Scan();
        if (!scan.IsComplete)
        {
            _log.Warn(
                $"启动恢复已跳过：音频会话枚举不完整。{scan.ErrorMessage}");
            return;
        }

        var liveKeys = scan.Sessions
            .Select(session => session.Key)
            .ToHashSet();
        var remainingRecords = new List<Models.RecoveryRecord>();

        foreach (var record in records)
        {
            if (liveKeys.Contains(record.Key)
                && record.MutedByTool
                && !record.OriginalMute
                && !_controller.TrySetMute(record.Key, muted: false))
            {
                remainingRecords.Add(record);
            }
        }

        if (remainingRecords.Count == 0)
        {
            if (!_journal.TryClear())
            {
                _log.Warn("启动恢复完成，但恢复日志清理失败。");
            }
        }
        else
        {
            if (!_journal.TrySave(remainingRecords))
            {
                _log.Warn("启动恢复未完成，且恢复日志更新失败。");
            }
        }
    }
}
