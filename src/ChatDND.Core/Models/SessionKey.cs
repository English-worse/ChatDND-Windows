namespace ChatDND.Core.Models;

public readonly record struct SessionKey(
    string SessionIdentifier,
    string SessionInstanceIdentifier,
    uint ProcessId);
