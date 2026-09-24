using ChatDND.Core.Models;

namespace ChatDND.Core.Services;

public static class DndSettingsUpdater
{
    public static DndSettings UpdateGeneralSettings(
        DndSettings current,
        bool autoEnableOnLaunch,
        bool minimizeToTrayOnStartup,
        bool runAsAdministratorAtStartup,
        int scanIntervalMs)
    {
        return current with
        {
            AutoEnableOnLaunch = autoEnableOnLaunch,
            MinimizeToTrayOnStartup = minimizeToTrayOnStartup,
            RunAsAdministratorAtStartup = runAsAdministratorAtStartup,
            ScanIntervalMs = Math.Clamp(scanIntervalMs, 100, 5000)
        };
    }
}
