using ChatDND.Core.Models;

namespace ChatDND.Core.Matching;

public static class AppRuleMatcher
{
    public static AppRule? Match(
        string? processPath,
        IEnumerable<AppRule>? rules)
    {
        if (string.IsNullOrWhiteSpace(processPath) || rules is null)
        {
            return null;
        }

        var processPathNormalized = ProcessPathNormalizer.TryNormalize(
            processPath,
            out var normalizedPath);
        var processFileName = GetFileName(processPath);

        foreach (var rule in rules.Where(rule => rule is { Enabled: true }))
        {
            if (rule.ExecutablePaths is null || rule.ExecutablePaths.Count == 0)
            {
                continue;
            }

            foreach (var candidatePath in rule.ExecutablePaths)
            {
                if (processPathNormalized
                    && ProcessPathNormalizer.TryNormalize(candidatePath, out var normalizedRulePath)
                    && string.Equals(
                        normalizedPath,
                        normalizedRulePath,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return rule;
                }

                if (!processPathNormalized
                    && string.Equals(
                        processFileName,
                        GetFileName(candidatePath),
                        StringComparison.OrdinalIgnoreCase))
                {
                    return rule;
                }
            }
        }

        return null;
    }

    private static string? GetFileName(string path)
    {
        try
        {
            return Path.GetFileName(path);
        }
        catch (Exception exception) when (
            exception is ArgumentException
            or NotSupportedException
            or PathTooLongException)
        {
            return null;
        }
    }
}
