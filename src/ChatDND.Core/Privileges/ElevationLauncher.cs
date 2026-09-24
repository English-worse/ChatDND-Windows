using System.Diagnostics;

namespace ChatDND.Core.Privileges;

public sealed class ElevationLauncher : IElevationLauncher
{
    private readonly string _executablePath;
    private readonly IProcessLauncher _launcher;

    public ElevationLauncher(string executablePath, IProcessLauncher launcher)
    {
        _executablePath = executablePath;
        _launcher = launcher;
    }

    public bool TryRestartElevated(bool resumeDnd)
    {
        var arguments = resumeDnd
            ? "--elevated --resume-dnd"
            : "--elevated";
        return _launcher.TryStart(
            _executablePath,
            arguments,
            "runas");
    }
}

public sealed class SystemProcessLauncher : IProcessLauncher
{
    public bool TryStart(string fileName, string arguments, string verb)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                Verb = verb,
                UseShellExecute = true
            }
        };

        try
        {
            return process.Start();
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }
}
