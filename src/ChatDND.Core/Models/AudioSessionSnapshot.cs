namespace ChatDND.Core.Models;

public sealed record AudioSessionSnapshot(
    SessionKey Key,
    string ProcessPath,
    bool IsMuted,
    SessionPlaybackState State);
