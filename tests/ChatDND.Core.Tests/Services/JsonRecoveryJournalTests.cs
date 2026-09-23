using ChatDND.Core.Models;
using ChatDND.Core.Services;

namespace ChatDND.Core.Tests.Services;

public sealed class JsonRecoveryJournalTests
{
    [Fact]
    public void SaveLoadAndClear_RoundTripsRecords()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var journal = new JsonRecoveryJournal(Path.Combine(directory, "recovery.json"));
        var record = new RecoveryRecord(
            new SessionKey("session", "instance", 42),
            @"C:\Apps\Chat.exe",
            OriginalMute: false,
            MutedByTool: true);

        journal.Save([record]);
        var loaded = journal.Load();
        journal.Clear();

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
        var journal = new JsonRecoveryJournal(path);

        var loaded = journal.Load();

        Assert.Empty(loaded);
        Assert.True(Directory.GetFiles(directory, "recovery.json.corrupt-*").Length == 1);
    }
}
