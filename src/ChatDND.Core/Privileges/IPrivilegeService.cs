namespace ChatDND.Core.Privileges;

public interface IPrivilegeService
{
    bool IsElevated { get; }
}
