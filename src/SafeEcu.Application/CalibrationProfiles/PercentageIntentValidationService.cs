namespace SafeEcu.Application.CalibrationProfiles;

public sealed class PercentageIntentValidationService
{
    private readonly CalibrationProfileCatalog _profileCatalog;

    public PercentageIntentValidationService(CalibrationProfileCatalog profileCatalog)
    {
        _profileCatalog = profileCatalog;
    }

    public PercentageIntentValidationResult Validate(PercentageIntentValidationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ProfileKey))
        {
            return Block(
                string.Empty,
                request.RequestedPercentage,
                CalibrationProfileSupportStatus.NotSupported,
                "Calibration profile is required.");
        }

        var profile = _profileCatalog.FindByKey(request.ProfileKey);
        if (profile is null)
        {
            return Block(
                request.ProfileKey,
                request.RequestedPercentage,
                CalibrationProfileSupportStatus.NotSupported,
                $"Calibration profile '{request.ProfileKey}' is unknown.");
        }

        if (profile.SupportStatus is not CalibrationProfileSupportStatus.Supported
            and not CalibrationProfileSupportStatus.Verified)
        {
            return Block(
                profile.Key,
                request.RequestedPercentage,
                profile.SupportStatus,
                $"Profile '{profile.DisplayName}' is {profile.SupportStatus} and cannot be applied.");
        }

        if (request.RequestedPercentage < profile.MinimumPercentage
            || request.RequestedPercentage > profile.MaximumPercentage)
        {
            return Block(
                profile.Key,
                request.RequestedPercentage,
                profile.SupportStatus,
                $"Requested percentage must be between {profile.MinimumPercentage}% and {profile.MaximumPercentage}% for '{profile.DisplayName}'.");
        }

        return new PercentageIntentValidationResult(
            true,
            profile.SupportStatus,
            profile.Key,
            request.RequestedPercentage,
            [
                $"Profile '{profile.DisplayName}' accepts the requested percentage.",
                profile.SafetyNote
            ],
            []);
    }

    private static PercentageIntentValidationResult Block(
        string profileKey,
        decimal requestedPercentage,
        CalibrationProfileSupportStatus supportStatus,
        string reason) =>
        new(
            false,
            supportStatus,
            profileKey,
            requestedPercentage,
            [],
            [reason]);
}
