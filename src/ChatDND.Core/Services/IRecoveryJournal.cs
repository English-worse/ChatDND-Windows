using ChatDND.Core.Models;

namespace ChatDND.Core.Services;

public interface IRecoveryJournal
{
    IReadOnlyList<RecoveryRecord> Load();

    bool TrySave(IReadOnlyCollection<RecoveryRecord> records);

    bool TryClear();
}
