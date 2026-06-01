using SafeEcu.Application.Checksums;
using SafeEcu.Application.EcuProfiles;
using SafeEcu.Application.Safety;

namespace SafeEcu.Application.CalibrationPatching;

public sealed record ValidatedCalibrationChangeSetRequest(
    VerifiedEcuSoftwareProfile Profile,
    CalibrationPatchResult PatchPreviewResult,
    EcuSoftwareChecksumSupportResult ChecksumSupport,
    CalibrationValidationResult SafetyValidation);
