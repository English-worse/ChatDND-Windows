namespace ChatDND.Core.Privileges;

public sealed class ElevationHandshake : IDisposable
{
    private const string EventPrefix = @"Local\ChatDND.Elevation.";
    private readonly EventWaitHandle _event;

    private ElevationHandshake(string token, EventWaitHandle eventHandle)
    {
        Token = token;
        _event = eventHandle;
    }

    public string Token { get; }

    public static ElevationHandshake Create()
    {
        var token = Guid.NewGuid().ToString("N");
        return new ElevationHandshake(
            token,
            new EventWaitHandle(
                initialState: false,
                EventResetMode.ManualReset,
                $"{EventPrefix}{token}"));
    }

    public static ElevationHandshake? TryOpen(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        try
        {
            return new ElevationHandshake(
                token,
                EventWaitHandle.OpenExisting($"{EventPrefix}{token}"));
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    public void SignalReady()
    {
        _event.Set();
    }

    public bool WaitForReady(TimeSpan timeout)
    {
        return _event.WaitOne(timeout);
    }

    public void Dispose()
    {
        _event.Dispose();
    }
}
