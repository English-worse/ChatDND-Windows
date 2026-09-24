namespace ChatDND.Core.Services;

public static class AppPaths
{
    public static string DataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ChatDND");

    public static string SettingsPath { get; } = Path.Combine(
        DataDirectory,
        "settings.json");

    public static string RecoveryPath { get; } = Path.Combine(
        DataDirectory,
        "recovery.json");

    public static string LogPath { get; } = Path.Combine(
        DataDirectory,
        "chatdnd.log");
}
