using ChatDND.Core.Audio;
using ChatDND.Core.Models;

namespace ChatDND.Core.Tests.Audio;

public sealed class WasapiAudioSessionAdapterTests
{
    [Fact]
    public void Provider_MapsEverySourceSession()
    {
        var source = new FakeCoreAudioSessionSource
        {
            Sessions =
            [
                Data(1),
                Data(2)
            ]
        };
        var provider = new WasapiAudioSessionProvider(source);

        var result = provider.Scan();

        Assert.True(result.IsComplete);
        Assert.Equal(2, result.Sessions.Count);
        Assert.Equal([1u, 2u], result.Sessions.Select(item => item.Key.ProcessId));
    }

    [Fact]
    public void Controller_DelegatesMuteAttemptToSource()
    {
        var source = new FakeCoreAudioSessionSource();
        var controller = new WasapiAudioSessionController(source);
        var key = Data(3).Key;

        var result = controller.TrySetMute(key, muted: true);

        Assert.True(result);
        Assert.Equal(key, source.LastMuteKey);
        Assert.True(source.LastMuteValue);
    }

    private static CoreAudioSessionData Data(uint processId)
    {
        return new CoreAudioSessionData(
            new SessionKey($"session-{processId}", $"instance-{processId}", processId),
            $@"C:\Apps\App{processId}.exe",
            IsMuted: false,
            SessionPlaybackState.Active);
    }

    private sealed class FakeCoreAudioSessionSource : ICoreAudioSessionSource
    {
        public IReadOnlyList<CoreAudioSessionData> Sessions { get; init; } = [];

        public SessionKey? LastMuteKey { get; private set; }

        public bool LastMuteValue { get; private set; }

        public bool IsComplete { get; init; } = true;

        public string? ErrorMessage { get; init; }

        public CoreAudioScanResult Enumerate()
        {
            return new CoreAudioScanResult(
                Sessions,
                IsComplete,
                ErrorMessage);
        }

        public bool TrySetMute(SessionKey sessionKey, bool muted)
        {
            LastMuteKey = sessionKey;
            LastMuteValue = muted;
            return true;
        }
    }
}
