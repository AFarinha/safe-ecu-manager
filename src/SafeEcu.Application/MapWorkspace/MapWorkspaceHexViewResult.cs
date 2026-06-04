namespace SafeEcu.Application.MapWorkspace;

public sealed record MapWorkspaceHexViewResult(
    bool IsSuccess,
    IReadOnlyList<MapWorkspaceHexRow> Rows,
    IReadOnlyList<string> Messages,
    IReadOnlyList<string> BlockReasons);
