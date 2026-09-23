using ChatDND.Core.Matching;
using ChatDND.Core.Models;

namespace ChatDND.Core.Tests.Matching;

public sealed class AppRuleMatcherTests
{
    private static readonly AppRule WeChat = new(
        "wechat",
        "微信",
        [@"C:\Program Files\Tencent\WeChat\WeChat.exe"]);

    [Fact]
    public void Match_UsesNormalizedFullPath()
    {
        var result = AppRuleMatcher.Match(
            @"c:\program files\tencent\wechat\wechat.exe",
            [WeChat]);

        Assert.Same(WeChat, result);
    }

    [Fact]
    public void Match_MatchesBareFilenameAgainstConfiguredRule()
    {
        var result = AppRuleMatcher.Match(
            "WeChat.exe",
            [WeChat]);

        Assert.Same(WeChat, result);
    }

    [Fact]
    public void Match_DoesNotMatchAnotherApplicationWithSameFilename()
    {
        var result = AppRuleMatcher.Match(
            @"D:\Portable\Other\WeChat.exe",
            [WeChat]);

        Assert.Null(result);
    }

    [Fact]
    public void Match_IgnoresDisabledRules()
    {
        var disabled = WeChat with { Enabled = false };

        var result = AppRuleMatcher.Match(
            @"C:\Program Files\Tencent\WeChat\WeChat.exe",
            [disabled]);

        Assert.Null(result);
    }

    [Fact]
    public void Match_DoesNotMatchUnlistedHelperProcess()
    {
        var result = AppRuleMatcher.Match(
            @"C:\Program Files\Common Files\SharedAudio.exe",
            [WeChat]);

        Assert.Null(result);
    }

    [Fact]
    public void Match_ReturnsNullForNullProcessPath()
    {
        var result = AppRuleMatcher.Match(null, [WeChat]);

        Assert.Null(result);
    }

    [Fact]
    public void Match_ReturnsNullForWhitespaceProcessPath()
    {
        var result = AppRuleMatcher.Match(" ", [WeChat]);

        Assert.Null(result);
    }

    [Fact]
    public void Match_ReturnsNullForNullRules()
    {
        var result = AppRuleMatcher.Match("WeChat.exe", null);

        Assert.Null(result);
    }

    [Fact]
    public void Match_ReturnsNullForNullExecutablePaths()
    {
        var rule = new AppRule("null-paths", "Null paths", null!);

        var result = AppRuleMatcher.Match(
            @"C:\Program Files\Tencent\WeChat\WeChat.exe",
            [rule]);

        Assert.Null(result);
    }

    [Fact]
    public void Match_ReturnsNullForEmptyExecutablePaths()
    {
        var rule = new AppRule("empty-paths", "Empty paths", []);

        var result = AppRuleMatcher.Match(
            @"C:\Program Files\Tencent\WeChat\WeChat.exe",
            [rule]);

        Assert.Null(result);
    }
}
