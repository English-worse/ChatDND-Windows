using ChatDND.Core.Models;

namespace ChatDND.Core.Audio;

public interface IAudioSessionController
{
    bool TrySetMute(SessionKey sessionKey, bool muted);
}
