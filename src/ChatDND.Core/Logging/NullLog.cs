namespace ChatDND.Core.Logging;

public sealed class NullLog : ILog
{
    public static NullLog Instance { get; } = new();

    private NullLog()
    {
    }

    public void Info(string message)
    {
    }

    public void Warn(string message)
    {
    }

    public void Error(string message)
    {
    }
}
