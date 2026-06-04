namespace SafeEcu.Application.Projects;

public sealed record EcuProjectMapExportGateResult(
    bool CanExport,
    IReadOnlyList<string> Messages,
    IReadOnlyList<string> BlockReasons);
