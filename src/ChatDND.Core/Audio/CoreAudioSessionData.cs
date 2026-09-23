using ChatDND.Core.Models;

namespace ChatDND.Core.Audio;

public sealed record CoreAudioSessionData(
    SessionKey Key,
    string ProcessPath,
    bool IsMuted,
    SessionPlaybackState State);
