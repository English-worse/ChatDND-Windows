using ChatDND.Core.Models;
using ChatDND.Core.Services;
using ChatDND.Core.Tests.Services.Fakes;

namespace ChatDND.Core.Tests.Services;

public sealed class DndBackgroundWorkerTests
{
    private static readonly AppRule WeChat = new(
        "wechat",
        "微信",
        [@"C:\Program Files\Tencent\WeChat\WeChat.exe"]);

    [Fact]
    public void Enable_ScansOnDedicatedWorkerThread()
    {
        var provider = new FakeAudioSessionProvider();
        var controller = new FakeAudioSessionController { Provider = provider };
        provider.Sessions.Add(Session(
            @"C:\Program Files\Tencent\WeChat\WeChat.exe",
            909));
        var coordinator = new DndCoordinator(
            provider,
            controller,
            new FakeRecoveryJournal());
        var recovery = new RecoveryService(
            provider,
            controller,
            new FakeRecoveryJournal(),
            new FakeLog());
        using var worker = new DndBackgroundWorker(
            coordinator,
            recovery,
            new FakeLog(),
            scanIntervalMs: 200);
        worker.Start();

        worker.Enable([WeChat]);

        Assert.True(provider.ScanSignal.Wait(TimeSpan.FromSeconds(3)));
        Assert.NotEqual(
            Environment.CurrentManagedThreadId,
            provider.LastScanThreadId);
    }

    [Fact]
    public void StopAndDisable_RestoresAndStopsBackgroundState()
    {
        var provider = new FakeAudioSessionProvider();
        var controller = new FakeAudioSessionController { Provider = provider };
        var session = Session(
            @"C:\Program Files\Tencent\WeChat\WeChat.exe",
            910);
        provider.Sessions.Add(session);
        var coordinator = new DndCoordinator(
            provider,
            controller,
            new FakeRecoveryJournal());
        var recovery = new RecoveryService(
            provider,
            controller,
            new FakeRecoveryJournal(),
            new FakeLog());
        using var worker = new DndBackgroundWorker(
            coordinator,
            recovery,
            new FakeLog(),
            scanIntervalMs: 200);
        worker.Start();
        worker.Enable([WeChat]);
        Assert.True(provider.ScanSignal.Wait(TimeSpan.FromSeconds(3)));

        var wasEnabled = worker.StopAndDisable();

        Assert.True(wasEnabled);
        Assert.False(worker.IsEnabled);
        Assert.False(controller.MuteStates[session.Key]);
    }

    private static AudioSessionSnapshot Session(string processPath, uint processId)
    {
        return new AudioSessionSnapshot(
            new SessionKey($"session-{processId}", $"instance-{processId}", processId),
            processPath,
            IsMuted: false,
            SessionPlaybackState.Active);
    }
}
