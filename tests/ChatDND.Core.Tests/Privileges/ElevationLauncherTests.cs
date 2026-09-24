using ChatDND.Core.Privileges;

namespace ChatDND.Core.Tests.Privileges;

public sealed class ElevationLauncherTests
{
    [Fact]
    public void TryRestartElevated_UsesRunasVerbAndElevatedArgument()
    {
        var launcher = new FakeProcessLauncher();
        var service = new ElevationLauncher(
            @"C:\Apps\ChatDND.exe",
            launcher);

        var result = service.TryRestartElevated(
            resumeDnd: true,
            handoffToken: "test-token");

        Assert.True(result);
        Assert.Equal("runas", launcher.Verb);
        Assert.Contains("--elevated", launcher.Arguments);
        Assert.Contains("--handoff=test-token", launcher.Arguments);
        Assert.Contains("--resume-dnd", launcher.Arguments);
    }

    [Fact]
    public void TryRestartElevated_WhenDndWasNotEnabled_DoesNotResumeDnd()
    {
        var launcher = new FakeProcessLauncher();
        var service = new ElevationLauncher(
            @"C:\Apps\ChatDND.exe",
            launcher);

        var result = service.TryRestartElevated(
            resumeDnd: false,
            handoffToken: "test-token");

        Assert.True(result);
        Assert.DoesNotContain("--resume-dnd", launcher.Arguments);
    }

    private sealed class FakeProcessLauncher : IProcessLauncher
    {
        public string? Verb { get; private set; }

        public string? Arguments { get; private set; }

        public bool TryStart(string fileName, string arguments, string verb)
        {
            Arguments = arguments;
            Verb = verb;
            return true;
        }
    }
}
