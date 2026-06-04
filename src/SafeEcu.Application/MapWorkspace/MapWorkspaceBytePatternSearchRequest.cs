namespace SafeEcu.Application.MapWorkspace;

public sealed record MapWorkspaceBytePatternSearchRequest(
    string FilePath,
    long StartOffset,
    int Length,
    string HexPattern);
