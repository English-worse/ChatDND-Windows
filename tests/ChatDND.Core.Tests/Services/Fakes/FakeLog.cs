using ChatDND.Core.Logging;

namespace ChatDND.Core.Tests.Services.Fakes;

internal sealed class FakeLog : ILog
{
    public List<string> Messages { get; } = [];

    public void Info(string message)
    {
        Messages.Add($"INFO:{message}");
    }

    public void Warn(string message)
    {
        Messages.Add($"WARN:{message}");
    }

    public void Error(string message)
    {
        Messages.Add($"ERROR:{message}");
    }
}
