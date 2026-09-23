using ChatDND.Core.Interop;

namespace ChatDND.Core.Tests.Interop;

public sealed class ProcessPathResolverTests
{
    [Fact]
    public void TryResolve_ReturnsPathForCurrentProcess()
    {
        var processId = (uint)Environment.ProcessId;

        var path = ProcessPathResolver.TryResolve(processId);

        Assert.NotNull(path);
        Assert.EndsWith(".exe", path, StringComparison.OrdinalIgnoreCase);
    }
}
