using ChatDND.Core.Audio;
using ChatDND.Core.Models;

namespace ChatDND.Core.Tests.Services.Fakes;

internal sealed class FakeAudioSessionController : IAudioSessionController
{
    public Dictionary<SessionKey, bool> MuteStates { get; } = [];

    public required FakeAudioSessionProvider Provider { get; init; }

    public bool TrySetMute(SessionKey sessionKey, bool muted)
    {
        MuteStates[sessionKey] = muted;
        var index = Provider.Sessions.FindIndex(item => item.Key == sessionKey);
        if (index < 0)
        {
            return false;
        }

        Provider.Sessions[index] = Provider.Sessions[index] with { IsMuted = muted };
        return true;
    }
}
