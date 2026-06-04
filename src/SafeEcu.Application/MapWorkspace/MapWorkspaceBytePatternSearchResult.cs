namespace SafeEcu.Application.MapWorkspace;

public sealed record MapWorkspaceBytePatternSearchResult(
    bool IsSuccess,
    IReadOnlyList<MapWorkspaceBytePatternMatch> Matches,
    IReadOnlyList<string> Messages,
    IReadOnlyList<string> BlockReasons);
