using ChatDND.Core.Privileges;

namespace ChatDND.Core.Tests.Privileges;

public sealed class PrivilegeServiceTests
{
    [Fact]
    public void IsElevated_UsesInjectedIdentity()
    {
        var service = new PrivilegeService(() => true);

        Assert.True(service.IsElevated);
    }
}
