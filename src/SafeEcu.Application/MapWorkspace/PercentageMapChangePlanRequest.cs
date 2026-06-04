using SafeEcu.Application.CalibrationPatching;

namespace SafeEcu.Application.MapWorkspace;

public sealed record PercentageMapChangePlanRequest(
    string OriginalFilePath,
    string ProfileKey,
    decimal RequestedPercentage,
    IReadOnlyList<CalibrationMapDefinition> MapDefinitions);
