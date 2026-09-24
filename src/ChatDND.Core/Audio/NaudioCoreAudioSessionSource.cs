using System.Diagnostics;
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

    public CoreAudioScanResult Enumerate()
    {
        var result = new List<CoreAudioSessionData>();
        var isComplete = true;
        string? errorMessage = null;

        try
        {
            using var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(
                DataFlow.Render,
                DeviceState.Active);

            foreach (var device in devices)
            {
                try
                {
                    using (device)
                    {
                        var sessions = device.AudioSessionManager.Sessions;
                        for (var index = 0; index < sessions.Count; index++)
                        {
                            if (!ReadSession(
                                sessions[index],
                                result,
                                out var sessionError))
                            {
                                isComplete = false;
                                errorMessage ??= sessionError;
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    isComplete = false;
                    errorMessage ??= exception.Message;
                    _log.Warn($"读取音频端点失败：{exception.Message}");
                }
            }
        }
        catch (Exception exception)
        {
            isComplete = false;
            errorMessage ??= exception.Message;
            _log.Warn($"枚举音频端点失败：{exception.Message}");
        }

        return new CoreAudioScanResult(result, isComplete, errorMessage);
    }

    public bool TrySetMute(SessionKey sessionKey, bool muted)
    {
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(
                DataFlow.Render,
                DeviceState.Active);

            foreach (var device in devices)
            {
                try
                {
                    using (device)
                    {
                        var sessions = device.AudioSessionManager.Sessions;
                        for (var index = 0; index < sessions.Count; index++)
                        {
                            using var session = sessions[index];
                            using var volume = session.SimpleAudioVolume;
                            var key = new SessionKey(
                                session.GetSessionIdentifier,
                                session.GetSessionInstanceIdentifier,
                                session.GetProcessID);
                            if (key == sessionKey)
                            {
                                volume.Mute = muted;
                                return true;
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    _log.Warn($"修改音频端点会话失败：{exception.Message}");
                }
            }
        }
        catch (Exception exception)
        {
            _log.Warn($"枚举音频端点失败：{exception.Message}");
        }

        return false;
    }

    private bool ReadSession(
        AudioSessionControl session,
        ICollection<CoreAudioSessionData> result,
        out string? errorMessage)
    {
        errorMessage = null;
        try
        {
            using var sessionScope = session;
            using var volume = session.SimpleAudioVolume;
            var state = MapState(session.State);
            if (state == SessionPlaybackState.Expired)
            {
                return true;
            }

            var processId = session.GetProcessID;
            var key = new SessionKey(
                session.GetSessionIdentifier,
                session.GetSessionInstanceIdentifier,
                processId);
            result.Add(new CoreAudioSessionData(
                key,
                ResolveProcessPath(processId),
                volume.Mute,
                state));
            return true;
        }
        catch (Exception exception)
        {
            errorMessage = exception.Message;
            _log.Warn($"读取音频会话失败：{exception.Message}");
            return false;
        }
    }

    private static string ResolveProcessPath(uint processId)
    {
        var fullPath = ProcessPathResolver.TryResolve(processId);
        if (!string.IsNullOrWhiteSpace(fullPath))
        {
            return fullPath;
        }

        try
        {
            using var process = Process.GetProcessById((int)processId);
            return $"{process.ProcessName}.exe";
        }
        catch (Exception)
        {
            return $"PID:{processId}";
        }
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
