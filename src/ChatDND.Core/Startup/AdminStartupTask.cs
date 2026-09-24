namespace ChatDND.Core.Startup;

public static class AdminStartupTask
{
    public const string TaskName = "ChatDND-AdminStartup";

    public static string BuildCreateArguments(string executablePath)
    {
        return $"/Create /TN \"{TaskName}\" /TR \"\\\"{executablePath}\\\"\" /SC ONLOGON /RL HIGHEST /F";
    }

    public static string BuildDeleteArguments()
    {
        return $"/Delete /TN \"{TaskName}\" /F";
    }
}
