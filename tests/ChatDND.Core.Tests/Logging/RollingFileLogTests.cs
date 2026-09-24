using ChatDND.Core.Logging;

namespace ChatDND.Core.Tests.Logging;

public sealed class RollingFileLogTests
{
    [Fact]
    public void Error_WritesTimestampedChineseMessageToFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "chatdnd.log");
        var log = new RollingFileLog(path, maxBytes: 4096);

        log.Error("测试错误");

        var text = File.ReadAllText(path);
        Assert.Contains("测试错误", text);
        Assert.Contains("[ERROR]", text);
    }

    [Fact]
    public void Info_WhenFileExceedsLimit_RollsToNumberedFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "chatdnd.log");
        var log = new RollingFileLog(path, maxBytes: 1);

        log.Info("first");
        log.Info("second");

        Assert.True(File.Exists(path));
        Assert.True(File.Exists($"{path}.1"));
    }
}
