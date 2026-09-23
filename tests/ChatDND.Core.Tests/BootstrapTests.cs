using ChatDND.Core;

namespace ChatDND.Core.Tests;

public sealed class BootstrapTests
{
    [Fact]
    public void CoreAssemblyLoads()
    {
        Assert.Equal("ChatDND.Core", typeof(Marker).Assembly.GetName().Name);
    }
}
