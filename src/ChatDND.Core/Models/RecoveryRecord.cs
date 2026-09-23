namespace ChatDND.Core.Models;

public sealed record RecoveryRecord(
    SessionKey Key,
    string ProcessPath,
    bool OriginalMute,
    bool MutedByTool);
