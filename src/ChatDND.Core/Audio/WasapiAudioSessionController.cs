using ChatDND.Core.Models;

namespace ChatDND.Core.Audio;

public sealed class WasapiAudioSessionController : IAudioSessionController
{
    private readonly ICoreAudioSessionSource _source;

    public WasapiAudioSessionController(ICoreAudioSessionSource source)
    {
        _source = source;
    }

    public bool TrySetMute(SessionKey sessionKey, bool muted)
    {
        return _source.TrySetMute(sessionKey, muted);
    }
}
