using ChatDND.Core.Audio;
using ChatDND.Core.Models;

namespace ChatDND.Core.Tests.Services.Fakes;

internal sealed class FakeAudioSessionController : IAudioSessionController
{
    public Dictionary<SessionKey, bool> MuteStates { get; } = [];

    public HashSet<SessionKey> FailingMuteKeys { get; } = [];

    public required FakeAudioSessionProvider Provider { get; init; }

    public bool TrySetMute(SessionKey sessionKey, bool muted)
    {
        var index = Provider.Sessions.FindIndex(item => item.Key == sessionKey);
        if (index < 0 || FailingMuteKeys.Contains(sessionKey))
        {
            return false;
        }

        MuteStates[sessionKey] = muted;
        Provider.Sessions[index] = Provider.Sessions[index] with { IsMuted = muted };
        return true;
    }
}
