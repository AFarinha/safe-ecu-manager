namespace SafeEcu.Application.MapWorkspace;

public sealed record MapWorkspacePreviewResult(
    bool IsSuccess,
    IReadOnlyList<MapWorkspacePreviewPoint> Points,
    string Unit,
    bool EditingAllowed,
    IReadOnlyList<string> Messages,
    IReadOnlyList<string> BlockReasons);
