using SafeEcu.Application.EcuProfiles;

namespace SafeEcu.Application.CalibrationPatching;

public sealed record CalibrationMapDefinitionValidationRequest(
    VerifiedEcuSoftwareProfile Profile,
    IReadOnlyList<CalibrationMapDefinition> MapDefinitions);
