using ChatDND.Core.Matching;

namespace ChatDND.Core.Tests.Matching;

public sealed class ProcessPathNormalizerTests
{
    [Theory]
    [InlineData(" C:\\Program Files\\Tencent\\WeChat\\WeChat.exe ")]
    [InlineData("\"C:\\Program Files\\Tencent\\WeChat\\WeChat.exe\"")]
    public void Normalize_RemovesQuotesAndWhitespace(string input)
    {
        var result = ProcessPathNormalizer.Normalize(input);

        Assert.Equal(@"C:\Program Files\Tencent\WeChat\WeChat.exe", result);
    }

    [Fact]
    public void TryNormalize_ReturnsFalseForEmptyPath()
    {
        var result = ProcessPathNormalizer.TryNormalize(" ", out var normalized);

        Assert.False(result);
        Assert.Equal(string.Empty, normalized);
    }

    [Fact]
    public void TryNormalize_ReturnsFalseForInvalidPath()
    {
        var result = ProcessPathNormalizer.TryNormalize(
            "C:\\invalid\0path.exe",
            out var normalized);

        Assert.False(result);
        Assert.Equal(string.Empty, normalized);
    }

    [Fact]
    public void TryNormalize_ReturnsFalseForBareFilename()
    {
        var result = ProcessPathNormalizer.TryNormalize(
            "WeChat.exe",
            out var normalized);

        Assert.False(result);
        Assert.Equal(string.Empty, normalized);
    }

    [Fact]
    public void Normalize_ThrowsForBareFilename()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => ProcessPathNormalizer.Normalize("WeChat.exe"));

        Assert.Contains("rooted", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
