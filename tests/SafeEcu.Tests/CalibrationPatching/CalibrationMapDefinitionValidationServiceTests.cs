using SafeEcu.Application.CalibrationPatching;
using SafeEcu.Application.EcuProfiles;

namespace SafeEcu.Tests.CalibrationPatching;

public sealed class CalibrationMapDefinitionValidationServiceTests
{
    [Fact]
    public void Validate_accepts_verified_dummy_profile_map_definition()
    {
        var service = new CalibrationMapDefinitionValidationService();

        var result = service.Validate(new CalibrationMapDefinitionValidationRequest(
            CreateProfile(),
            [CreateMap()]));

        Assert.True(result.IsAllowed);
        Assert.Empty(result.BlockReasons);
    }

    [Fact]
    public void Validate_blocks_real_profile_in_this_phase()
    {
        var service = new CalibrationMapDefinitionValidationService();
        var profile = CreateProfile(profileId: "opel-y17dt-real-candidate", source: "Workshop evidence");

        var result = service.Validate(new CalibrationMapDefinitionValidationRequest(profile, [CreateMap(profileId: profile.ProfileId)]));

        Assert.False(result.IsAllowed);
        Assert.Contains("Only dummy/test", result.BlockReasons.Single());
    }

    [Fact]
    public void Validate_blocks_map_not_allowed_by_profile()
    {
        var service = new CalibrationMapDefinitionValidationService();

        var result = service.Validate(new CalibrationMapDefinitionValidationRequest(
            CreateProfile(),
            [CreateMap(mapId: "UnknownMap")]));

        Assert.False(result.IsAllowed);
        Assert.Contains("not allowed", result.BlockReasons.Single());
    }

    [Fact]
    public void Validate_blocks_unknown_data_type()
    {
        var service = new CalibrationMapDefinitionValidationService();

        var result = service.Validate(new CalibrationMapDefinitionValidationRequest(
            CreateProfile(),
            [CreateMap(dataType: CalibrationMapDataType.Unknown)]));

        Assert.False(result.IsAllowed);
        Assert.Contains("unknown data type", result.BlockReasons.Single());
    }

    [Fact]
    public void Validate_blocks_emissions_related_map()
    {
        var service = new CalibrationMapDefinitionValidationService();

        var result = service.Validate(new CalibrationMapDefinitionValidationRequest(
            CreateProfile(allowedMapIds: ["EgrSwitch"]),
            [CreateMap(mapId: "EgrSwitch", isEmissionsRelated: true)]));

        Assert.False(result.IsAllowed);
        Assert.Contains("emissions-related", result.BlockReasons.Single());
    }

    private static VerifiedEcuSoftwareProfile CreateProfile(
        string profileId = "dummy-test-profile",
        string source = "Unit test",
        IReadOnlyList<string>? allowedMapIds = null) =>
        new(
            profileId,
            "Dummy ECU",
            "HW-DUMMY",
            "SW-DUMMY",
            "1.0",
            4,
            new string('a', 64),
            allowedMapIds ?? ["FuelQuantity"],
            "dummy-checksum",
            VerifiedEcuSoftwareSupportStatus.Verified,
            source,
            "Dummy-only profile.");

    private static CalibrationMapDefinition CreateMap(
        string mapId = "FuelQuantity",
        string profileId = "dummy-test-profile",
        CalibrationMapDataType dataType = CalibrationMapDataType.UInt8,
        bool isEmissionsRelated = false) =>
        new(
            mapId,
            mapId,
            mapId,
            new CalibrationMapAddressRange(0, 4),
            isEmissionsRelated,
            [],
            profileId,
            dataType,
            CalibrationMapEndianess.NotApplicable,
            1m,
            "raw",
            0m,
            255m,
            CalibrationMapDefinitionSupportStatus.Verified);
}
