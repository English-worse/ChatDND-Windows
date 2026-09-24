using ChatDND.Core.Models;
using ChatDND.Core.Services;
using ChatDND.Core.Tests.Services.Fakes;

namespace ChatDND.Core.Tests.Services;

public sealed class JsonRecoveryJournalTests
{
    [Fact]
    public void SaveLoadAndClear_RoundTripsRecords()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var journal = new JsonRecoveryJournal(
            Path.Combine(directory, "recovery.json"),
            new FakeLog());
        var record = new RecoveryRecord(
            new SessionKey("session", "instance", 42),
            @"C:\Apps\Chat.exe",
            OriginalMute: false,
            MutedByTool: true);

        Assert.True(journal.TrySave([record]));
        var loaded = journal.Load();
        Assert.True(journal.TryClear());

        Assert.Single(loaded);
        Assert.False(loaded[0].OriginalMute);
        Assert.Empty(journal.Load());
    }

    [Fact]
    public void Load_WhenJsonIsCorrupt_BacksUpAndReturnsEmpty()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "recovery.json");
        File.WriteAllText(path, "{ broken json");
        var journal = new JsonRecoveryJournal(path, new FakeLog());

        var loaded = journal.Load();

        Assert.Empty(loaded);
        Assert.True(Directory.GetFiles(directory, "recovery.json.corrupt-*").Length == 1);
    }

    [Fact]
    public void TrySave_WhenDirectoryCannotBeCreated_ReturnsFalseAndLogs()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var blockedDirectory = Path.Combine(directory, "blocked");
        File.WriteAllText(blockedDirectory, "not a directory");
        var log = new FakeLog();
        var journal = new JsonRecoveryJournal(
            Path.Combine(blockedDirectory, "recovery.json"),
            log);
        var record = new RecoveryRecord(
            new SessionKey("session", "instance", 42),
            @"C:\Apps\Chat.exe",
            OriginalMute: false,
            MutedByTool: true);

        var saved = journal.TrySave([record]);

        Assert.False(saved);
        Assert.Contains(log.Messages, message => message.StartsWith("ERROR:"));
    }
}
