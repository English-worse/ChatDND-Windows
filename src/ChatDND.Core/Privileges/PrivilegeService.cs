namespace ChatDND.Core.Privileges;

public sealed class PrivilegeService : IPrivilegeService
{
    private readonly Func<bool> _isElevated;

    public PrivilegeService(Func<bool>? isElevated = null)
    {
        _isElevated = isElevated ?? Detect;
    }

    public bool IsElevated => _isElevated();

    private static bool Detect()
    {
        using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
        var principal = new System.Security.Principal.WindowsPrincipal(identity);
        return principal.IsInRole(
            System.Security.Principal.WindowsBuiltInRole.Administrator);
    }
}
