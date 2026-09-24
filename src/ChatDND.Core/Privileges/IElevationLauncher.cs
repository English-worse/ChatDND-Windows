namespace ChatDND.Core.Privileges;

public interface IElevationLauncher
{
    bool TryRestartElevated();
}

public interface IProcessLauncher
{
    bool TryStart(string fileName, string arguments, string verb);
}
