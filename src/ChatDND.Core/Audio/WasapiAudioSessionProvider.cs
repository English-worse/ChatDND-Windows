using ChatDND.Core.Models;

namespace ChatDND.Core.Audio;

public sealed class WasapiAudioSessionProvider : IAudioSessionProvider
{
    private readonly ICoreAudioSessionSource _source;

    public WasapiAudioSessionProvider(ICoreAudioSessionSource source)
    {
        _source = source;
    }

    public IReadOnlyList<AudioSessionSnapshot> GetSessions()
    {
        return _source.Enumerate()
            .Select(CoreAudioSessionMapper.ToSnapshot)
            .ToArray();
    }
}
