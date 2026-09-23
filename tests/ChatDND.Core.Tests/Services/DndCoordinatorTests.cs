using ChatDND.Core.Models;
using ChatDND.Core.Services;
using ChatDND.Core.Tests.Services.Fakes;

namespace ChatDND.Core.Tests.Services;

public sealed class DndCoordinatorTests
{
    private static readonly AppRule WeChat = new(
        "wechat",
        "微信",
        [@"C:\Program Files\Tencent\WeChat\WeChat.exe"]);

    [Fact]
    public void Enable_MutesEveryMatchingInstance()
    {
        var provider = new FakeAudioSessionProvider();
        var controller = new FakeAudioSessionController { Provider = provider };
        var first = Session(@"C:\Program Files\Tencent\WeChat\WeChat.exe", 101);
        var second = Session(@"C:\Program Files\Tencent\WeChat\WeChat.exe", 202);
        provider.Sessions.AddRange([first, second]);
        var coordinator = new DndCoordinator(
            provider,
            controller,
            new FakeRecoveryJournal());

        var result = coordinator.Enable([WeChat]);

        Assert.Equal(DndEnableResult.Enabled, result);
        Assert.True(controller.MuteStates[first.Key]);
        Assert.True(controller.MuteStates[second.Key]);
    }

    [Fact]
    public void Enable_WithNoRules_DoesNotEnterDnd()
    {
        var provider = new FakeAudioSessionProvider();
        var coordinator = new DndCoordinator(
            provider,
            new FakeAudioSessionController { Provider = provider },
            new FakeRecoveryJournal());

        var result = coordinator.Enable([]);

        Assert.Equal(DndEnableResult.NoRules, result);
        Assert.False(coordinator.IsEnabled);
    }

    [Fact]
    public void Disable_RestoresOnlyOriginallyUnmutedSessions()
    {
        var provider = new FakeAudioSessionProvider();
        var controller = new FakeAudioSessionController { Provider = provider };
        var originallyMuted = Session(
            @"C:\Program Files\Tencent\WeChat\WeChat.exe",
            101,
            isMuted: true);
        var originallyAudible = Session(
            @"C:\Program Files\Tencent\WeChat\WeChat.exe",
            202,
            isMuted: false);
        provider.Sessions.AddRange([originallyMuted, originallyAudible]);
        var coordinator = new DndCoordinator(
            provider,
            controller,
            new FakeRecoveryJournal());
        coordinator.Enable([WeChat]);

        coordinator.Disable();

        Assert.False(controller.MuteStates.ContainsKey(originallyMuted.Key));
        Assert.False(controller.MuteStates[originallyAudible.Key]);
    }

    [Fact]
    public void Tick_MutesSessionAppearingAfterEnable()
    {
        var provider = new FakeAudioSessionProvider();
        var controller = new FakeAudioSessionController { Provider = provider };
        var coordinator = new DndCoordinator(
            provider,
            controller,
            new FakeRecoveryJournal());
        coordinator.Enable([WeChat]);
        var laterSession = Session(
            @"C:\Program Files\Tencent\WeChat\WeChat.exe",
            303);
        provider.Sessions.Add(laterSession);

        coordinator.Tick([WeChat]);

        Assert.True(controller.MuteStates[laterSession.Key]);
    }

    private static AudioSessionSnapshot Session(
        string processPath,
        uint processId,
        bool isMuted = false)
    {
        return new AudioSessionSnapshot(
            new SessionKey($"session-{processId}", $"instance-{processId}", processId),
            processPath,
            isMuted,
            SessionPlaybackState.Active);
    }
}
