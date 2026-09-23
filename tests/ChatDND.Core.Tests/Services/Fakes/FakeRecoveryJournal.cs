using ChatDND.Core.Models;
using ChatDND.Core.Services;

namespace ChatDND.Core.Tests.Services.Fakes;

internal sealed class FakeRecoveryJournal : IRecoveryJournal
{
    public List<RecoveryRecord> Records { get; private set; } = [];

    public IReadOnlyList<RecoveryRecord> Load()
    {
        return Records.ToArray();
    }

    public void Save(IReadOnlyCollection<RecoveryRecord> records)
    {
        Records = [.. records];
    }

    public void Clear()
    {
        Records = [];
    }
}
