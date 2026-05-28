using SafeEcu.Application.CalibrationProfiles;

namespace SafeEcu.Application.GuidedProfiles;

public sealed class GuidedProfileService
{
    private readonly GuidedCalibrationProfileCatalog _catalog;

    public GuidedProfileService(GuidedCalibrationProfileCatalog catalog)
    {
        _catalog = catalog;
    }

    public IReadOnlyList<GuidedCalibrationProfile> ListProfiles() => _catalog.ListAll();

    public GuidedProfileAvailability EvaluateAvailability(string profileKey)
    {
        if (string.IsNullOrWhiteSpace(profileKey))
        {
            return new GuidedProfileAvailability(string.Empty, false, [], ["Guided profile is required."]);
        }

        var profile = _catalog.FindByKey(profileKey);
        if (profile is null)
        {
            return new GuidedProfileAvailability(profileKey, false, [], [$"Guided profile '{profileKey}' is unknown."]);
        }

        if (profile.SupportStatus is not CalibrationProfileSupportStatus.Supported
            and not CalibrationProfileSupportStatus.Verified)
        {
            return new GuidedProfileAvailability(
                profile.Key,
                false,
                [profile.UserDescription],
                [$"Guided profile '{profile.Name}' is {profile.SupportStatus} and is blocked."]);
        }

        return new GuidedProfileAvailability(
            profile.Key,
            true,
            [profile.UserDescription, "Profile is available for non-writing workflows only."],
            []);
    }
}
