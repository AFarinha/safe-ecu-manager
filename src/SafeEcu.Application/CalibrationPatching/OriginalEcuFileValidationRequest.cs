using SafeEcu.Application.EcuProfiles;

namespace SafeEcu.Application.CalibrationPatching;

public sealed record OriginalEcuFileValidationRequest(
    string OriginalFilePath,
    string BackupFilePath,
    VerifiedEcuSoftwareProfile Profile,
    IReadOnlyList<CalibrationMapDefinition> MapDefinitions);
