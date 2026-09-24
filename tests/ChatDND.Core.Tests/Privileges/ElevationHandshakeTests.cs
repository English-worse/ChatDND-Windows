using ChatDND.Core.Privileges;

namespace ChatDND.Core.Tests.Privileges;

public sealed class ElevationHandshakeTests
{
    [Fact]
    public async Task WaitForReadyAsync_ReturnsTrueAfterSignal()
    {
        using var handshake = ElevationHandshake.Create();

        var wait = handshake.WaitForReadyAsync(TimeSpan.FromSeconds(2));
        handshake.SignalReady();

        Assert.True(await wait);
    }
}
