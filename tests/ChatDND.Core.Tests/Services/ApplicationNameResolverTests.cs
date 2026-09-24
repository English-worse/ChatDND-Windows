using ChatDND.Core.Services;

namespace ChatDND.Core.Tests.Services;

public sealed class ApplicationNameResolverTests
{
    [Theory]
    [InlineData(@"C:\Program Files\Tencent\WeChat\WeChat.exe", "微信")]
    [InlineData(@"C:\Program Files\Tencent\QQ\QQ.exe", "QQ")]
    [InlineData(@"C:\Program Files\DingTalk\DingTalk.exe", "钉钉")]
    [InlineData(@"C:\Program Files\Tencent\WeCom\WXWork.exe", "企业微信")]
    [InlineData(@"C:\Program Files\Feishu\Feishu.exe", "飞书")]
    public void Resolve_KnownChatApplication_ReturnsChineseName(
        string processPath,
        string expectedName)
    {
        var result = new KnownApplicationNameResolver().Resolve(processPath);

        Assert.True(result.IsKnown);
        Assert.Equal(expectedName, result.DisplayName);
    }

    [Fact]
    public void Resolve_UnknownApplication_ReturnsChineseFallback()
    {
        var result = new KnownApplicationNameResolver().Resolve(
            @"C:\Applications\UnknownApp.exe");

        Assert.False(result.IsKnown);
        Assert.Contains("未识别程序", result.DisplayName);
        Assert.Contains("UnknownApp.exe", result.DisplayName);
    }
}
