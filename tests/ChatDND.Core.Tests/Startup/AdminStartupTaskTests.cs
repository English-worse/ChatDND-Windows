using ChatDND.Core.Startup;

namespace ChatDND.Core.Tests.Startup;

public sealed class AdminStartupTaskTests
{
    [Fact]
    public void BuildCreateArguments_UsesLogonAndHighestPrivileges()
    {
        var arguments = AdminStartupTask.BuildCreateArguments(
            @"C:\Program Files\ChatDND\ChatDND.App.exe");

        Assert.Contains("/Create", arguments);
        Assert.Contains("/SC ONLOGON", arguments);
        Assert.Contains("/RL HIGHEST", arguments);
        Assert.Contains(@"C:\Program Files\ChatDND\ChatDND.App.exe", arguments);
    }

    [Fact]
    public void BuildDeleteArguments_TargetsChatDndTask()
    {
        var arguments = AdminStartupTask.BuildDeleteArguments();

        Assert.Contains("/Delete", arguments);
        Assert.Contains(AdminStartupTask.TaskName, arguments);
    }
}
