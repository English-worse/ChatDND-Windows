using ChatDND.Core.Services;

namespace ChatDND.Core.Tests.Services;

public sealed class AppPathsTests
{
    [Fact]
    public void Paths_AreInsideLocalApplicationData()
    {
        var local = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);

        Assert.StartsWith(local, AppPaths.DataDirectory, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("settings.json", AppPaths.SettingsPath, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("recovery.json", AppPaths.RecoveryPath, StringComparison.OrdinalIgnoreCase);
    }
}
