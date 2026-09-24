using ChatDND.App;
using ChatDND.Core.Models;

namespace ChatDND.Core.Tests.App;

public sealed class SettingsFormTests
{
    [Fact]
    public void Constructor_ChecksOnlyRecognizedApplications()
    {
        var known = new CandidateApp(
            "微信",
            @"C:\Program Files\Tencent\WeChat\WeChat.exe",
            IsKnown: true);
        var unknown = new CandidateApp(
            "未识别程序（Game.exe）",
            @"C:\Games\Game.exe");

        using var form = new SettingsForm([known, unknown]);

        var selected = Assert.Single(form.SelectedCandidates);
        Assert.Equal(known, selected);
    }
}
