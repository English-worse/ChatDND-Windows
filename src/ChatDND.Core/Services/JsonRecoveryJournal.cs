using System.Text.Json;
using ChatDND.Core.Models;

namespace ChatDND.Core.Services;

public sealed class JsonRecoveryJournal : IRecoveryJournal
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly string _path;

    public JsonRecoveryJournal(string path)
    {
        _path = path;
    }

    public IReadOnlyList<RecoveryRecord> Load()
    {
        if (!File.Exists(_path))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<RecoveryRecord[]>(json, Options) ?? [];
        }
        catch (JsonException)
        {
            BackupCorruptFile();
            return [];
        }
        catch (NotSupportedException)
        {
            BackupCorruptFile();
            return [];
        }
        catch (IOException)
        {
            return [];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
    }

    public void Save(IReadOnlyCollection<RecoveryRecord> records)
    {
        var directory = Path.GetDirectoryName(_path)
            ?? throw new InvalidOperationException("Recovery path has no directory.");
        Directory.CreateDirectory(directory);
        var temporaryPath = $"{_path}.tmp";

        try
        {
            File.WriteAllText(
                temporaryPath,
                JsonSerializer.Serialize(records, Options));
            File.Move(temporaryPath, _path, overwrite: true);
        }
        finally
        {
            try
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    public void Clear()
    {
        try
        {
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private void BackupCorruptFile()
    {
        try
        {
            File.Move(
                _path,
                $"{_path}.corrupt-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                overwrite: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
