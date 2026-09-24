using ChatDND.Core.Models;

namespace ChatDND.Core.Audio;

public interface ICoreAudioSessionSource
{
    CoreAudioScanResult Enumerate();

    bool TrySetMute(SessionKey sessionKey, bool muted);
}
