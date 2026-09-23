using ChatDND.Core.Models;

namespace ChatDND.Core.Services;

public interface IRecoveryJournal
{
    IReadOnlyList<RecoveryRecord> Load();

    void Save(IReadOnlyCollection<RecoveryRecord> records);

    void Clear();
}
