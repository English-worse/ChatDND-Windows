namespace ChatDND.Core.Logging;

public sealed class RollingFileLog : ILog
{
    private readonly string _path;
    private readonly long _maxBytes;
    private readonly object _gate = new();

    public RollingFileLog(string path, long maxBytes = 1_048_576)
    {
        _path = path;
        _maxBytes = maxBytes;
    }

    public void Info(string message)
    {
        Write("INFO", message);
    }

    public void Warn(string message)
    {
        Write("WARN", message);
    }

    public void Error(string message)
    {
        Write("ERROR", message);
    }

    private void Write(string level, string message)
    {
        lock (_gate)
        {
            var directory = Path.GetDirectoryName(_path)
                ?? throw new InvalidOperationException("Log path has no directory.");
            Directory.CreateDirectory(directory);

            if (File.Exists(_path) && new FileInfo(_path).Length >= _maxBytes)
            {
                File.Move(_path, $"{_path}.1", overwrite: true);
            }

            File.AppendAllText(
                _path,
                $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}");
        }
    }
}
