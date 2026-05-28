using SafeEcu.Domain.Vehicles;

namespace SafeEcu.Application.EcuProfiles;

public sealed class EcuFamilyProfileService
{
    private readonly EcuFamilyProfileCatalog _catalog;

    public EcuFamilyProfileService(EcuFamilyProfileCatalog catalog)
    {
        _catalog = catalog;
    }

    public IReadOnlyList<EcuFamilyProfile> ListProfiles() => _catalog.Profiles;

    public EcuFamilyProfile? FindBestMatch(EcuInfo ecuInfo)
    {
        if (string.IsNullOrWhiteSpace(ecuInfo.EcuFamily))
        {
            return null;
        }

        return _catalog.Profiles.FirstOrDefault(profile =>
            ecuInfo.EcuFamily.Contains(profile.FamilyName, StringComparison.OrdinalIgnoreCase) ||
            profile.FamilyName.Contains(ecuInfo.EcuFamily, StringComparison.OrdinalIgnoreCase));
    }
}
