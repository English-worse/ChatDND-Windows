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
            _journal.Clear();
        }
        else
        {
            _journal.Save(remainingRecords);
        }
    }
}
