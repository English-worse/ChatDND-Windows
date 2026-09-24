using ChatDND.App;

namespace ChatDND.Core.Tests.App;

public sealed class IconFactoryTests
{
    [Fact]
    public void LoadAppIcon_ReturnsUsableIcon()
    {
        using var icon = IconFactory.LoadAppIcon();

        Assert.True(icon.Width > 0);
        Assert.True(icon.Height > 0);
    }
}
