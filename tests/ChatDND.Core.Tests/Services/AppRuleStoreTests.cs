using ChatDND.Core.Models;
using ChatDND.Core.Services;

namespace ChatDND.Core.Tests.Services;

public sealed class AppRuleStoreTests
{
    [Fact]
    public void SaveAndLoad_RoundTripsRules()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "settings.json");
        var store = new AppRuleStore(path);
        var settings = DndSettings.Default with
        {
            AutoEnableOnLaunch = false,
            Rules =
            [
                new AppRule(
                    "wechat",
                    "微信",
                    [@"C:\Program Files\Tencent\WeChat\WeChat.exe"])
            ]
        };

        store.Save(settings);
        var loaded = store.Load();

        Assert.False(loaded.AutoEnableOnLaunch);
        Assert.Single(loaded.Rules);
        Assert.Equal("wechat", loaded.Rules[0].Id);
    }

    [Fact]
    public void Load_WhenJsonIsCorrupt_BacksUpAndReturnsDefaults()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "settings.json");
        File.WriteAllText(path, "{ broken json");
        var store = new AppRuleStore(path);

        var loaded = store.Load();

        Assert.True(loaded.AutoEnableOnLaunch);
        Assert.Equal(500, loaded.ScanIntervalMs);
        Assert.Empty(loaded.Rules);
        Assert.True(Directory.GetFiles(directory, "settings.json.corrupt-*").Length == 1);
    }

    [Fact]
    public void Load_ClampsInvalidScanInterval()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "settings.json");
        File.WriteAllText(
            path,
            """
            {
              "autoEnableOnLaunch": true,
              "minimizeToTrayOnStartup": true,
              "runAsAdministratorAtStartup": false,
              "scanIntervalMs": 0,
              "rules": []
            }
            """);

        var loaded = new AppRuleStore(path).Load();

        Assert.Equal(100, loaded.ScanIntervalMs);
    }
}
