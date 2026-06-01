using SafeEcu.Application.EcuProfiles;

namespace SafeEcu.Application.CalibrationPatching;

public sealed class ValidatedCalibrationChangeSetService
{
    public ValidatedCalibrationChangeSetResult Create(ValidatedCalibrationChangeSetRequest request)
    {
        var messages = new List<string>();
        var blockReasons = new List<string>();

        if (request.Profile.SupportStatus is not VerifiedEcuSoftwareSupportStatus.Supported
            and not VerifiedEcuSoftwareSupportStatus.Verified)
        {
            blockReasons.Add($"ECU software profile is {request.Profile.SupportStatus}; Supported or Verified is required.");
        }

        if (!request.PatchPreviewResult.IsAllowed)
        {
            blockReasons.AddRange(request.PatchPreviewResult.BlockReasons);
        }

        if (request.PatchPreviewResult.PreviewItems is null || request.PatchPreviewResult.PreviewItems.Count == 0)
        {
            blockReasons.Add("Patch preview must locate at least one map value.");
        }

        if (!request.ChecksumSupport.CanExportCalibrationOutput)
        {
            blockReasons.AddRange(request.ChecksumSupport.BlockReasons);
        }

        if (!request.SafetyValidation.IsAllowed)
        {
            blockReasons.AddRange(request.SafetyValidation.BlockReasons);
        }

        if (blockReasons.Count > 0)
        {
            return new ValidatedCalibrationChangeSetResult(false, null, messages, blockReasons);
        }

        messages.Add("Validated calibration change set is ready for non-export review. No modified file was created.");
        return new ValidatedCalibrationChangeSetResult(
            true,
            request.PatchPreviewResult.ChangeSet with { CreatesModifiedFile = false },
            messages,
            []);
    }
}
