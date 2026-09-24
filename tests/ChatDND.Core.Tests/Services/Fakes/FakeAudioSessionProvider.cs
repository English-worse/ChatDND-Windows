using ChatDND.Core.Audio;
using ChatDND.Core.Models;

namespace ChatDND.Core.Tests.Services.Fakes;

internal sealed class FakeAudioSessionProvider : IAudioSessionProvider
{
    public List<AudioSessionSnapshot> Sessions { get; } = [];

    public bool IsComplete { get; set; } = true;

    public string? ErrorMessage { get; set; }

    public int ScanCount { get; private set; }

    public int LastScanThreadId { get; private set; }

    public ManualResetEventSlim ScanSignal { get; } = new(initialState: false);

    public AudioSessionScanResult Scan()
    {
        ScanCount++;
        LastScanThreadId = Environment.CurrentManagedThreadId;
        ScanSignal.Set();
        return new AudioSessionScanResult(
            Sessions.ToArray(),
            IsComplete,
            ErrorMessage);
    }

    public IReadOnlyList<AudioSessionSnapshot> GetSessions()
    {
        return Sessions.ToArray();
    }
}
