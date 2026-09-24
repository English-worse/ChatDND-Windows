using System.Text.Json;
using ChatDND.Core.Logging;
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
    private readonly ILog _log;

    public JsonRecoveryJournal(string path, ILog log)
    {
        _path = path;
        _log = log;
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

    public bool TrySave(IReadOnlyCollection<RecoveryRecord> records)
    {
        try
        {
            var directory = Path.GetDirectoryName(_path);
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new InvalidOperationException("Recovery path has no directory.");
            }

            Directory.CreateDirectory(directory);
            var temporaryPath = $"{_path}.tmp";
            File.WriteAllText(
                temporaryPath,
                JsonSerializer.Serialize(records, Options));
            File.Move(temporaryPath, _path, overwrite: true);
            return true;
        }
        catch (Exception exception) when (
            exception is IOException
            or UnauthorizedAccessException
            or InvalidOperationException
            or JsonException)
        {
            _log.Error($"写入恢复日志失败：{exception.Message}");
            CleanupTemporaryFile();
            return false;
        }
    }

    public bool TryClear()
    {
        try
        {
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }

            return true;
        }
        catch (Exception exception) when (
            exception is IOException
            or UnauthorizedAccessException)
        {
            _log.Error($"清理恢复日志失败：{exception.Message}");
            return false;
        }
    }

    private void CleanupTemporaryFile()
    {
        var temporaryPath = $"{_path}.tmp";
        try
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (IOException)
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
