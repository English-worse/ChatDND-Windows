using ChatDND.Core.Audio;
using ChatDND.Core.Logging;
using ChatDND.Core.Privileges;
using ChatDND.Core.Services;

namespace ChatDND.App;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        using var singleInstance = new SingleInstanceGuard();
        if (!singleInstance.TryAcquire())
        {
            return;
        }

        var log = new RollingFileLog(AppPaths.LogPath);
        var launchedElevated = args.Contains("--elevated", StringComparer.OrdinalIgnoreCase);
        var handoffToken = GetArgumentValue(args, "--handoff=");
        using var handshake = ElevationHandshake.TryOpen(handoffToken);
        var source = new NaudioCoreAudioSessionSource(log);
        var provider = new WasapiAudioSessionProvider(source);
        var controller = new WasapiAudioSessionController(source);
        var journal = new JsonRecoveryJournal(AppPaths.RecoveryPath, log);
        var store = new AppRuleStore(AppPaths.SettingsPath);
        var coordinator = new DndCoordinator(provider, controller, journal);
        var recovery = new RecoveryService(provider, controller, journal, log);
        var discovery = new ProcessDiscoveryService(provider);
        var adminStartup = new WindowsAdminStartupService();
        var appController = new AppController(
            coordinator,
            recovery,
            store,
            discovery,
            adminStartup,
            log);

        var resumeDnd = args.Contains("--resume-dnd", StringComparer.OrdinalIgnoreCase)
            && appController.Settings.Rules.Any(rule => rule.Enabled);
        if (launchedElevated)
        {
            appController.Start(autoEnable: false);
        }
        else
        {
            appController.Start();
        }

        if (resumeDnd)
        {
            appController.Enable();
        }

        handshake?.SignalReady();

        var elevationLauncher = new ElevationLauncher(
            Application.ExecutablePath,
            new SystemProcessLauncher());
        var isCurrentlyElevated = new PrivilegeService().IsElevated;
        var context = new TrayApplicationContext(
            appController,
            singleInstance,
            elevationLauncher,
            isCurrentlyElevated);
        if (appController.Settings.Rules.Count == 0
            || !appController.Settings.MinimizeToTrayOnStartup)
        {
            context.ShowMainForm();
        }

        Application.Run(context);
    }

    private static string? GetArgumentValue(string[] args, string prefix)
    {
        var value = args.FirstOrDefault(
            argument => argument.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        return value is null ? null : value[prefix.Length..];
    }
}
