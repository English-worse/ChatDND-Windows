using ChatDND.Core.Models;

namespace ChatDND.Core.Audio;

public static class CoreAudioSessionMapper
{
    public static AudioSessionSnapshot ToSnapshot(CoreAudioSessionData data)
    {
        return new AudioSessionSnapshot(
            data.Key,
            data.ProcessPath,
            data.IsMuted,
            data.State);
    }
}
