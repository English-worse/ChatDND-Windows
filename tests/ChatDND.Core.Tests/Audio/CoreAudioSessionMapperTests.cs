using ChatDND.Core.Audio;
using ChatDND.Core.Models;

namespace ChatDND.Core.Tests.Audio;

public sealed class CoreAudioSessionMapperTests
{
    [Fact]
    public void ToSnapshot_PreservesSessionIdentityAndMuteState()
    {
        var data = new CoreAudioSessionData(
            new SessionKey("session", "instance", 42),
            @"C:\Apps\Chat.exe",
            IsMuted: true,
            SessionPlaybackState.Active);

        var snapshot = CoreAudioSessionMapper.ToSnapshot(data);

        Assert.Equal(data.Key, snapshot.Key);
        Assert.Equal(data.ProcessPath, snapshot.ProcessPath);
        Assert.True(snapshot.IsMuted);
        Assert.Equal(SessionPlaybackState.Active, snapshot.State);
    }
}
