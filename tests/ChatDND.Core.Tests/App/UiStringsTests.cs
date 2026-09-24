using ChatDND.App;

namespace ChatDND.Core.Tests.App;

public sealed class UiStringsTests
{
    [Fact]
    public void PrimaryLabels_AreChinese()
    {
        Assert.Equal("应用级免打扰", UiStrings.AppTitle);
        Assert.Equal("开启免打扰", UiStrings.EnableDnd);
        Assert.Equal("退出程序", UiStrings.ExitApplication);
        Assert.Equal("删除选中规则", UiStrings.RemoveSelectedRule);
        Assert.Equal("设置", UiStrings.Settings);
        Assert.Equal("启动时自动开启免打扰", UiStrings.AutoEnableOnLaunch);
        Assert.Equal("启动后最小化到托盘", UiStrings.MinimizeToTrayOnStartup);
        Assert.Equal("以管理员身份运行（提高兼容性，存在系统权限风险）", UiStrings.RunElevated);
    }
}
