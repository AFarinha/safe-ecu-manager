namespace SafeEcu.Application.Projects;

public sealed record EcuProjectMapEditPreviewResult(
    bool IsAllowed,
    IReadOnlyList<EcuProjectMapEditPreviewCell> Cells,
    IReadOnlyList<string> Messages,
    IReadOnlyList<string> BlockReasons);
