using ChatDND.Core.Models;

namespace ChatDND.Core.Audio;

public interface ICoreAudioSessionSource
{
    IReadOnlyList<CoreAudioSessionData> Enumerate();

    bool TrySetMute(SessionKey sessionKey, bool muted);
}
