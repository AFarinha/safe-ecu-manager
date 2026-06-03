using SafeEcu.Application.EcuProfiles;
using SafeEcu.Domain.Vehicles;

namespace SafeEcu.Tests.EcuProfiles;

public sealed class EcuFamilyProfileServiceTests
{
    [Fact]
    public void Catalog_contains_initial_ecu_family_profiles_without_verified_support()
    {
        var profiles = new EcuFamilyProfileCatalog().Profiles;

        Assert.Equal("Delphi DCM", profiles[0].FamilyName);
        Assert.Contains(profiles, profile => profile.FamilyName == "Bosch EDC15");
        Assert.Contains(profiles, profile => profile.FamilyName == "Delco/Delphi/Isuzu Opel 1.7 DTI");
        Assert.DoesNotContain(profiles, profile => profile.SupportLevel == EcuFamilySupportLevel.Verified);
    }

    [Fact]
    public void FindBestMatch_matches_renault_megane_candidate_family()
    {
        var service = new EcuFamilyProfileService(new EcuFamilyProfileCatalog());

        var match = service.FindBestMatch(new EcuInfo
        {
            EcuFamily = "Delphi DCM"
        });

        Assert.NotNull(match);
        Assert.Equal(EcuFamilySupportLevel.FileManagement, match.SupportLevel);
    }

    [Fact]
    public void FindBestMatch_matches_by_ecu_family_text()
    {
        var service = new EcuFamilyProfileService(new EcuFamilyProfileCatalog());

        var match = service.FindBestMatch(new EcuInfo
        {
            EcuFamily = "Delco/Delphi/Isuzu Opel 1.7 DTI"
        });

        Assert.NotNull(match);
        Assert.Equal(EcuFamilySupportLevel.FileManagement, match.SupportLevel);
    }

    [Fact]
    public void Unknown_family_returns_no_match()
    {
        var service = new EcuFamilyProfileService(new EcuFamilyProfileCatalog());

        var match = service.FindBestMatch(new EcuInfo());

        Assert.Null(match);
    }
}
