using ChatDND.Core.Models;

namespace ChatDND.Core.Audio;

public sealed class WasapiAudioSessionProvider : IAudioSessionProvider
{
    private readonly ICoreAudioSessionSource _source;

    public WasapiAudioSessionProvider(ICoreAudioSessionSource source)
    {
        _source = source;
    }

    public AudioSessionScanResult Scan()
    {
        var result = _source.Enumerate();
        return new AudioSessionScanResult(
            result.Sessions
                .Select(CoreAudioSessionMapper.ToSnapshot)
                .ToArray(),
            result.IsComplete,
            result.ErrorMessage);
    }
}
