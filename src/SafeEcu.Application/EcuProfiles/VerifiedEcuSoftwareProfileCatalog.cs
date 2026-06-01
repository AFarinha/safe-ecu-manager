namespace SafeEcu.Application.EcuProfiles;

public sealed class VerifiedEcuSoftwareProfileCatalog
{
    private static readonly IReadOnlyList<VerifiedEcuSoftwareProfile> EmptyProfiles = [];

    public IReadOnlyList<VerifiedEcuSoftwareProfile> Profiles => EmptyProfiles;
}
