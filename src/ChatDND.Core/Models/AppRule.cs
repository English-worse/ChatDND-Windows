namespace ChatDND.Core.Models;

public sealed record AppRule(
    string Id,
    string DisplayName,
    IReadOnlyList<string> ExecutablePaths,
    bool Enabled = true);
