using ChatDND.Core.Models;
using ChatDND.Core.Services;
using ChatDND.Core.Tests.Services.Fakes;

namespace ChatDND.Core.Tests.Services;

public sealed class RecoveryServiceTests
{
    [Fact]
    public void Recover_UnmutesOnlySessionsChangedByTheTool()
    {
        var provider = new FakeAudioSessionProvider();
        var controller = new FakeAudioSessionController { Provider = provider };
        var key = new SessionKey("session", "instance", 42);
        provider.Sessions.Add(new AudioSessionSnapshot(
            key,
            @"C:\Apps\Chat.exe",
            IsMuted: true,
            SessionPlaybackState.Active));
        var journal = new FakeRecoveryJournal();
        journal.Save([
            new RecoveryRecord(key, @"C:\Apps\Chat.exe", OriginalMute: false, MutedByTool: true)
        ]);
        var service = new RecoveryService(provider, controller, journal);

        service.Recover();

        Assert.False(controller.MuteStates[key]);
        Assert.Empty(journal.Load());
    }

    [Fact]
    public void Recover_LeavesOriginallyMutedSessionsMuted()
    {
        var provider = new FakeAudioSessionProvider();
        var controller = new FakeAudioSessionController { Provider = provider };
        var key = new SessionKey("session", "instance", 42);
        provider.Sessions.Add(new AudioSessionSnapshot(
            key,
            @"C:\Apps\Chat.exe",
            IsMuted: true,
            SessionPlaybackState.Active));
        var journal = new FakeRecoveryJournal();
        journal.Save([
            new RecoveryRecord(key, @"C:\Apps\Chat.exe", OriginalMute: true, MutedByTool: true)
        ]);

        new RecoveryService(provider, controller, journal).Recover();

        Assert.Empty(controller.MuteStates);
        Assert.Empty(journal.Load());
    }

    [Fact]
    public void Recover_WhenUnmuteFails_KeepsJournalRecord()
    {
        var provider = new FakeAudioSessionProvider();
        var controller = new FakeAudioSessionController { Provider = provider };
        var key = new SessionKey("session", "instance", 42);
        provider.Sessions.Add(new AudioSessionSnapshot(
            key,
            @"C:\Apps\Chat.exe",
            IsMuted: true,
            SessionPlaybackState.Active));
        controller.FailingMuteKeys.Add(key);
        var journal = new FakeRecoveryJournal();
        var record = new RecoveryRecord(
            key,
            @"C:\Apps\Chat.exe",
            OriginalMute: false,
            MutedByTool: true);
        journal.Save([record]);

        new RecoveryService(provider, controller, journal).Recover();

        Assert.Single(journal.Load());
    }
}
