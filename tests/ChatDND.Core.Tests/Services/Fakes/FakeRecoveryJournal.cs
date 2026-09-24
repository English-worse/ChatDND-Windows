using ChatDND.Core.Models;
using ChatDND.Core.Services;

namespace ChatDND.Core.Tests.Services.Fakes;

internal sealed class FakeRecoveryJournal : IRecoveryJournal
{
    public List<RecoveryRecord> Records { get; private set; } = [];

    public int SaveCount { get; private set; }

    public int ClearCount { get; private set; }

    public bool FailSave { get; set; }

    public bool FailClear { get; set; }

    public IReadOnlyList<RecoveryRecord> Load()
    {
        return Records.ToArray();
    }

    public bool TrySave(IReadOnlyCollection<RecoveryRecord> records)
    {
        SaveCount++;
        if (FailSave)
        {
            return false;
        }

        Records = [.. records];
        return true;
    }

    public bool TryClear()
    {
        ClearCount++;
        if (FailClear)
        {
            return false;
        }

        Records = [];
        return true;
    }
}
