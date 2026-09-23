using System.Text.Json;
using ChatDND.Core.Models;

namespace ChatDND.Core.Services;

public sealed class AppRuleStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public AppRuleStore(string settingsPath)
    {
        SettingsPath = settingsPath;
    }

    public string SettingsPath { get; }

    public DndSettings Load()
    {
        if (!File.Exists(SettingsPath))
        {
            return DndSettings.Default;
        }

        try
        {
            var json = File.ReadAllText(SettingsPath);
            return Normalize(
                JsonSerializer.Deserialize<DndSettings>(json, Options)
                    ?? DndSettings.Default);
        }
        catch (JsonException)
        {
            BackupCorruptFile();
            return DndSettings.Default;
        }
        catch (NotSupportedException)
        {
            BackupCorruptFile();
            return DndSettings.Default;
        }
    }

    public void Save(DndSettings settings)
    {
        var directory = Path.GetDirectoryName(SettingsPath)
            ?? throw new InvalidOperationException("Settings path has no directory.");
        Directory.CreateDirectory(directory);

        var temporaryPath = $"{SettingsPath}.tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, Options));
        File.Move(temporaryPath, SettingsPath, overwrite: true);
    }

    private void BackupCorruptFile()
    {
        var backupPath = $"{SettingsPath}.corrupt-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        File.Move(SettingsPath, backupPath, overwrite: true);
    }

    private static DndSettings Normalize(DndSettings settings)
    {
        return settings with
        {
            ScanIntervalMs = Math.Clamp(settings.ScanIntervalMs, 100, 5000),
            Rules = settings.Rules ?? []
        };
    }
}
