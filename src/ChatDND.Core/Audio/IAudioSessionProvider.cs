using ChatDND.Core.Models;

namespace ChatDND.Core.Audio;

public interface IAudioSessionProvider
{
    IReadOnlyList<AudioSessionSnapshot> GetSessions();
}
