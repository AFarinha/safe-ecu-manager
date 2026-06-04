using SafeEcu.Application.CalibrationPatching;

namespace SafeEcu.Application.MapWorkspace;

public sealed record MapDefinitionCsvImportResult(
    bool IsSuccess,
    IReadOnlyList<CalibrationMapDefinition> MapDefinitions,
    IReadOnlyList<string> Messages,
    IReadOnlyList<string> Errors);
