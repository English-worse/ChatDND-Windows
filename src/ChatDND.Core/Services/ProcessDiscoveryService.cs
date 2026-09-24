using ChatDND.Core.Audio;
using ChatDND.Core.Matching;
using ChatDND.Core.Models;

namespace ChatDND.Core.Services;

public sealed class ProcessDiscoveryService
{
    private readonly IAudioSessionProvider _provider;
    private readonly KnownApplicationNameResolver _nameResolver;

    public ProcessDiscoveryService(
        IAudioSessionProvider provider,
        KnownApplicationNameResolver? nameResolver = null)
    {
        _provider = provider;
        _nameResolver = nameResolver ?? new KnownApplicationNameResolver();
    }

    public IReadOnlyList<CandidateApp> Discover()
    {
        var currentProcessId = (uint)Environment.ProcessId;
        var candidates = new Dictionary<string, CandidateApp>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var session in _provider.Scan().Sessions)
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
                var resolved = _nameResolver.Resolve(session.ProcessPath);
                candidates[normalizedPath] = new CandidateApp(
                    resolved.DisplayName,
                    session.ProcessPath,
                    resolved.IsKnown);
            }
        }

        return candidates.Values
            .OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
