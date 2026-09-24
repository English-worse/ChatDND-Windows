namespace ChatDND.Core.Audio;

public sealed record CoreAudioScanResult(
    IReadOnlyList<CoreAudioSessionData> Sessions,
    bool IsComplete,
    string? ErrorMessage = null);
