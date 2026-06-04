using SafeEcu.Application.CalibrationPatching;

namespace SafeEcu.Application.MapWorkspace;

public sealed record MapChangeExportRequest(
    string OriginalFilePath,
    string OutputDirectory,
    string OutputFileName,
    IReadOnlyList<CalibrationMapDefinition> MapDefinitions,
    IReadOnlyList<CalibrationChange> Changes,
    MapChangeExportGateResult ExportGate,
    bool ChecksumRecalculated);
