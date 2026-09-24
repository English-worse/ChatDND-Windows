namespace ChatDND.Core.Privileges;

public interface IElevationLauncher
{
    bool TryRestartElevated(bool resumeDnd);
}

public interface IProcessLauncher
{
    bool TryStart(string fileName, string arguments, string verb);
}
