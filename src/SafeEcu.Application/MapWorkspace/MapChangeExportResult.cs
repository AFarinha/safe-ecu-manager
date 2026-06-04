namespace SafeEcu.Application.MapWorkspace;

public sealed record MapChangeExportResult(
    bool IsSuccess,
    string? OutputFilePath,
    string? Sha256Hash,
    IReadOnlyList<string> Messages,
    IReadOnlyList<string> BlockReasons);
