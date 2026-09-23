using ChatDND.Core.Audio;
using ChatDND.Core.Models;

namespace ChatDND.Core.Tests.Services.Fakes;

internal sealed class FakeAudioSessionProvider : IAudioSessionProvider
{
    public List<AudioSessionSnapshot> Sessions { get; } = [];

    public IReadOnlyList<AudioSessionSnapshot> GetSessions()
    {
        return Sessions.ToArray();
    }
}
