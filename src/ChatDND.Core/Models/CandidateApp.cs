namespace ChatDND.Core.Models;

public sealed record CandidateApp(
    string DisplayName,
    string ProcessPath,
    bool IsKnown = false)
{
    public string DisplayText => IsKnown
        ? $"{DisplayName}（{Path.GetFileName(ProcessPath)}）"
        : DisplayName;
}
