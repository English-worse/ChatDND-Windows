namespace ChatDND.Core.Models;

public sealed record DndSettings(
    bool AutoEnableOnLaunch,
    bool MinimizeToTrayOnStartup,
    bool RunAsAdministratorAtStartup,
    int ScanIntervalMs,
    IReadOnlyList<AppRule> Rules)
{
    public static DndSettings Default { get; } = new(
        AutoEnableOnLaunch: true,
        MinimizeToTrayOnStartup: true,
        RunAsAdministratorAtStartup: false,
        ScanIntervalMs: 500,
        Rules: []);
}
