using ChatDND.Core.Interop;
using ChatDND.Core.Logging;
using ChatDND.Core.Models;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace ChatDND.Core.Audio;

public sealed class NaudioCoreAudioSessionSource : ICoreAudioSessionSource
{
    private readonly ILog _log;

    public NaudioCoreAudioSessionSource(ILog log)
    {
        _log = log;
    }

    public IReadOnlyList<CoreAudioSessionData> Enumerate()
    {
        var result = new List<CoreAudioSessionData>();
        using var enumerator = new MMDeviceEnumerator();
        var devices = enumerator.EnumerateAudioEndPoints(
            DataFlow.Render,
            DeviceState.Active);

        try
        {
            foreach (var device in devices)
            {
                using (device)
                {
                    var sessions = device.AudioSessionManager.Sessions;
                    for (var index = 0; index < sessions.Count; index++)
                    {
                        var session = sessions[index];
                        var processId = session.GetProcessID;
                        var processPath = ProcessPathResolver.TryResolve(processId)
                            ?? $"PID:{processId}";
                        var key = new SessionKey(
                            session.GetSessionIdentifier,
                            session.GetSessionInstanceIdentifier,
                            processId);
                        result.Add(new CoreAudioSessionData(
                            key,
                            processPath,
                            session.SimpleAudioVolume.Mute,
                            MapState(session.State)));
                    }
                }
            }
        }
        catch (Exception exception)
        {
            _log.Warn($"枚举音频会话失败：{exception.Message}");
        }

        return result;
    }

    public bool TrySetMute(SessionKey sessionKey, bool muted)
    {
        using var enumerator = new MMDeviceEnumerator();
        var devices = enumerator.EnumerateAudioEndPoints(
            DataFlow.Render,
            DeviceState.Active);

        try
        {
            foreach (var device in devices)
            {
                using (device)
                {
                    var sessions = device.AudioSessionManager.Sessions;
                    for (var index = 0; index < sessions.Count; index++)
                    {
                        var session = sessions[index];
                        var key = new SessionKey(
                            session.GetSessionIdentifier,
                            session.GetSessionInstanceIdentifier,
                            session.GetProcessID);
                        if (key == sessionKey)
                        {
                            session.SimpleAudioVolume.Mute = muted;
                            return true;
                        }
                    }
                }
            }
        }
        catch (Exception exception)
        {
            _log.Warn($"修改音频会话失败：{exception.Message}");
        }

        return false;
    }

    private static SessionPlaybackState MapState(AudioSessionState state)
    {
        return state switch
        {
            AudioSessionState.AudioSessionStateActive => SessionPlaybackState.Active,
            AudioSessionState.AudioSessionStateExpired => SessionPlaybackState.Expired,
            _ => SessionPlaybackState.Inactive
        };
    }
}
