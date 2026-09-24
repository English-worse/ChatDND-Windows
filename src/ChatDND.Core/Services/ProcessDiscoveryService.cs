using ChatDND.Core.Audio;
using ChatDND.Core.Matching;
using ChatDND.Core.Models;

namespace ChatDND.Core.Services;

public sealed class ProcessDiscoveryService
{
    private readonly IAudioSessionProvider _provider;

    public ProcessDiscoveryService(IAudioSessionProvider provider)
    {
        _provider = provider;
    }

    public IReadOnlyList<CandidateApp> Discover()
    {
        var currentProcessId = (uint)Environment.ProcessId;
        var candidates = new Dictionary<string, CandidateApp>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var session in _provider.GetSessions())
        {
            if (session.Key.ProcessId == currentProcessId
                || !ProcessPathNormalizer.TryNormalize(
                    session.ProcessPath,
                    out var normalizedPath))
            {
                continue;
            }

            if (!candidates.ContainsKey(normalizedPath))
            {
                var displayName = Path.GetFileNameWithoutExtension(
                    session.ProcessPath.Trim().Trim('"'));
                candidates[normalizedPath] = new CandidateApp(
                    displayName,
                    session.ProcessPath);
            }
        }

        return candidates.Values
            .OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
