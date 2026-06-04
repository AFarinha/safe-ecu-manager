using SafeEcu.Application.CalibrationPatching;

namespace SafeEcu.Application.MapWorkspace;

public sealed record DefinedMapComparisonRequest(
    string OriginalFilePath,
    string ModifiedFilePath,
    CalibrationMapDefinition MapDefinition);
