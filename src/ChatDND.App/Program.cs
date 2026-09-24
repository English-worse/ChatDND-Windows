using ChatDND.Core.Audio;
using ChatDND.Core.Logging;
using ChatDND.Core.Services;

namespace ChatDND.App;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        var log = new RollingFileLog(AppPaths.LogPath);
        var source = new NaudioCoreAudioSessionSource(log);
        var provider = new WasapiAudioSessionProvider(source);
        var controller = new WasapiAudioSessionController(source);
        var journal = new JsonRecoveryJournal(AppPaths.RecoveryPath);
        var store = new AppRuleStore(AppPaths.SettingsPath);
        var coordinator = new DndCoordinator(provider, controller, journal);
        var recovery = new RecoveryService(provider, controller, journal);
        var discovery = new ProcessDiscoveryService(provider);
        var appController = new AppController(
            coordinator,
            recovery,
            store,
            discovery,
            log);

        if (args.Contains("--resume-dnd", StringComparer.OrdinalIgnoreCase))
        {
            appController.Enable();
        }
        else
        {
            appController.Start();
        }

        var context = new TrayApplicationContext(appController);
        if (appController.Settings.Rules.Count == 0)
        {
            context.ShowMainForm();
        }

        Application.Run(context);
    }
}
