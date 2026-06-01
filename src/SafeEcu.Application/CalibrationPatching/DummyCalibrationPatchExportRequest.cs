using SafeEcu.Application.EcuProfiles;

namespace SafeEcu.Application.CalibrationPatching;

public sealed record DummyCalibrationPatchExportRequest(
    string OriginalFilePath,
    string OutputDirectory,
    VerifiedEcuSoftwareProfile Profile,
    ValidatedCalibrationChangeSetResult ValidatedChangeSet);
