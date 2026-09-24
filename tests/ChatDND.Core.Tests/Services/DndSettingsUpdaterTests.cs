using ChatDND.Core.Models;
using ChatDND.Core.Services;

namespace ChatDND.Core.Tests.Services;

public sealed class DndSettingsUpdaterTests
{
    [Fact]
    public void UpdateGeneralSettings_PreservesRulesAndClampsInterval()
    {
        var current = DndSettings.Default with
        {
            Rules =
            [
                new AppRule("wechat", "微信", [@"C:\WeChat.exe"])
            ]
        };

        var updated = DndSettingsUpdater.UpdateGeneralSettings(
            current,
            autoEnableOnLaunch: false,
            minimizeToTrayOnStartup: false,
            runAsAdministratorAtStartup: true,
            scanIntervalMs: 20);

        Assert.False(updated.AutoEnableOnLaunch);
        Assert.False(updated.MinimizeToTrayOnStartup);
        Assert.True(updated.RunAsAdministratorAtStartup);
        Assert.Equal(100, updated.ScanIntervalMs);
        Assert.Single(updated.Rules);
        Assert.Equal("wechat", updated.Rules[0].Id);
    }
}
