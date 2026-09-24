namespace ChatDND.Core.Models;

public sealed record AudioSessionScanResult(
    IReadOnlyList<AudioSessionSnapshot> Sessions,
    bool IsComplete,
    string? ErrorMessage = null);
