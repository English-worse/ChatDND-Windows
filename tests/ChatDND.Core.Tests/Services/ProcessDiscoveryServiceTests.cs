using ChatDND.Core.Models;
using ChatDND.Core.Services;
using ChatDND.Core.Tests.Services.Fakes;

namespace ChatDND.Core.Tests.Services;

public sealed class ProcessDiscoveryServiceTests
{
    [Fact]
    public void Discover_ReturnsDistinctApplicationsAndSkipsCurrentProcess()
    {
        var provider = new FakeAudioSessionProvider();
        provider.Sessions.AddRange([
            Session(@"C:\Program Files\Tencent\WeChat\WeChat.exe", 10),
            Session(@"c:\program files\tencent\wechat\wechat.exe", 11),
            Session(@"C:\Program Files\DingTalk\DingTalk.exe", 12),
            Session(@"C:\Tools\ChatDND.exe", (uint)Environment.ProcessId)
        ]);
        var service = new ProcessDiscoveryService(provider);

        var candidates = service.Discover();

        Assert.Equal(2, candidates.Count);
        Assert.Contains(
            candidates,
            item => item.DisplayName == "微信" && item.IsKnown);
        Assert.Contains(
            candidates,
            item => item.DisplayName == "钉钉" && item.IsKnown);
        Assert.Contains(
            candidates,
            item => item.DisplayText.StartsWith("微信", StringComparison.Ordinal));
    }

    private static AudioSessionSnapshot Session(string path, uint pid)
    {
        return new AudioSessionSnapshot(
            new SessionKey($"session-{pid}", $"instance-{pid}", pid),
            path,
            IsMuted: false,
            SessionPlaybackState.Active);
    }
}
