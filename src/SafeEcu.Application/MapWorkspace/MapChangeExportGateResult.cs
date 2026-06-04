namespace SafeEcu.Application.MapWorkspace;

public sealed record MapChangeExportGateResult(
    bool CanExport,
    IReadOnlyList<string> Messages,
    IReadOnlyList<string> BlockReasons);
