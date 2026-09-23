using ChatDND.Core.Models;

namespace ChatDND.Core.Matching;

public static class AppRuleMatcher
{
    public static AppRule? Match(string processPath, IEnumerable<AppRule> rules)
    {
        var processPathNormalized = ProcessPathNormalizer.TryNormalize(
            processPath,
            out var normalizedPath);

        foreach (var rule in rules.Where(rule => rule.Enabled))
        {
            foreach (var candidatePath in rule.ExecutablePaths)
            {
                if (processPathNormalized
                    && ProcessPathNormalizer.TryNormalize(candidatePath, out var normalizedRulePath)
                    && normalizedPath == normalizedRulePath)
                {
                    return rule;
                }

                if (!processPathNormalized
                    && string.Equals(
                        Path.GetFileName(processPath),
                        Path.GetFileName(candidatePath),
                        StringComparison.OrdinalIgnoreCase))
                {
                    return rule;
                }
            }
        }

        return null;
    }
}
