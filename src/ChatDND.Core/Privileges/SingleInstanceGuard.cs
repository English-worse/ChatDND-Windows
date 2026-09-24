namespace ChatDND.Core.Privileges;

public sealed class SingleInstanceGuard : IDisposable
{
    private readonly Mutex _mutex;
    private bool _isOwner;

    public SingleInstanceGuard(string name = @"Local\ChatDND.SingleInstance")
    {
        _mutex = new Mutex(initiallyOwned: false, name);
    }

    public bool IsOwner => _isOwner;

    public bool TryAcquire()
    {
        if (_isOwner)
        {
            return true;
        }

        try
        {
            _isOwner = _mutex.WaitOne(0);
        }
        catch (AbandonedMutexException)
        {
            _isOwner = true;
        }

        return _isOwner;
    }

    public void Release()
    {
        if (!_isOwner)
        {
            return;
        }

        _mutex.ReleaseMutex();
        _isOwner = false;
    }

    public void Dispose()
    {
        Release();
        _mutex.Dispose();
    }
}
