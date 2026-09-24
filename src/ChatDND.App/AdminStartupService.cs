using System.Diagnostics;
using ChatDND.Core.Startup;

namespace ChatDND.App;

public interface IAdminStartupService
{
    bool TryEnable();

    bool TryDisable();
}

public sealed class WindowsAdminStartupService : IAdminStartupService
{
    public bool TryEnable()
    {
        return RunScheduledTask(
            AdminStartupTask.BuildCreateArguments(Application.ExecutablePath));
    }

    public bool TryDisable()
    {
        return RunScheduledTask(AdminStartupTask.BuildDeleteArguments());
    }

    private static bool RunScheduledTask(string arguments)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        try
        {
            return process.Start()
                && process.WaitForExit(10_000)
                && process.ExitCode == 0;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }
}
