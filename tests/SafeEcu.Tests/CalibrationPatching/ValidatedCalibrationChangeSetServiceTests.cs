using SafeEcu.Application.CalibrationPatching;
using SafeEcu.Application.Checksums;
using SafeEcu.Application.EcuProfiles;
using SafeEcu.Application.Safety;

namespace SafeEcu.Tests.CalibrationPatching;

public sealed class ValidatedCalibrationChangeSetServiceTests
{
    [Fact]
    public void Create_allows_changeset_only_when_all_gates_pass()
    {
        var result = new ValidatedCalibrationChangeSetService().Create(new ValidatedCalibrationChangeSetRequest(
            CreateProfile(),
            CreatePatchResult(isAllowed: true),
            CreateChecksumSupport(canExport: true),
            CreateSafetyValidation(isAllowed: true)));

        Assert.True(result.IsAllowed);
        Assert.NotNull(result.ChangeSet);
        Assert.False(result.ChangeSet!.CreatesModifiedFile);
    }

    [Fact]
    public void Create_blocks_when_profile_is_not_supported_or_verified()
    {
        var result = new ValidatedCalibrationChangeSetService().Create(new ValidatedCalibrationChangeSetRequest(
            CreateProfile(VerifiedEcuSoftwareSupportStatus.Experimental),
            CreatePatchResult(isAllowed: true),
            CreateChecksumSupport(canExport: true),
            CreateSafetyValidation(isAllowed: true)));

        Assert.False(result.IsAllowed);
        Assert.Null(result.ChangeSet);
        Assert.Contains("Supported or Verified", result.BlockReasons.Single());
    }

    [Fact]
    public void Create_blocks_when_checksum_export_is_not_supported()
    {
        var result = new ValidatedCalibrationChangeSetService().Create(new ValidatedCalibrationChangeSetRequest(
            CreateProfile(),
            CreatePatchResult(isAllowed: true),
            CreateChecksumSupport(canExport: false),
            CreateSafetyValidation(isAllowed: true)));

        Assert.False(result.IsAllowed);
        Assert.Contains("Checksum missing", result.BlockReasons.Single());
    }

    [Fact]
    public void Create_blocks_when_patch_preview_did_not_locate_maps()
    {
        var result = new ValidatedCalibrationChangeSetService().Create(new ValidatedCalibrationChangeSetRequest(
            CreateProfile(),
            CreatePatchResult(isAllowed: true, includePreviewItems: false),
            CreateChecksumSupport(canExport: true),
            CreateSafetyValidation(isAllowed: true)));

        Assert.False(result.IsAllowed);
        Assert.Contains("locate at least one", result.BlockReasons.Single());
    }

    [Fact]
    public void Create_blocks_when_safety_validation_fails()
    {
        var result = new ValidatedCalibrationChangeSetService().Create(new ValidatedCalibrationChangeSetRequest(
            CreateProfile(),
            CreatePatchResult(isAllowed: true),
            CreateChecksumSupport(canExport: true),
            CreateSafetyValidation(isAllowed: false)));

        Assert.False(result.IsAllowed);
        Assert.Contains("Unsafe", result.BlockReasons.Single());
    }

    private static VerifiedEcuSoftwareProfile CreateProfile(
        VerifiedEcuSoftwareSupportStatus supportStatus = VerifiedEcuSoftwareSupportStatus.Verified) =>
        new(
            "dummy-test-profile",
            "Dummy ECU",
            "HW-DUMMY",
            "SW-DUMMY",
            "1.0",
            4,
            new string('a', 64),
            ["FuelQuantity"],
            "dummy-checksum",
            supportStatus,
            "Unit test",
            "Dummy-only profile.");

    private static CalibrationPatchResult CreatePatchResult(bool isAllowed, bool includePreviewItems = true) =>
        new(
            isAllowed,
            isAllowed ? CalibrationPatchStatus.AllowedPreview : CalibrationPatchStatus.Blocked,
            new CalibrationChangeSet(
                "technical-comparison",
                CalibrationPatchMode.DummyPatch,
                [new CalibrationMapValue("FuelQuantity", 1, 0x20, 0x25)],
                CreatesModifiedFile: false),
            isAllowed ? ["Preview OK"] : [],
            isAllowed ? [] : ["Preview blocked"],
            includePreviewItems
                ? [new CalibrationPatchPreviewItem("FuelQuantity", "Fuel Quantity", "FuelQuantity", 1, 0x20, 0x25, [], "Dummy")]
                : []);

    private static EcuSoftwareChecksumSupportResult CreateChecksumSupport(bool canExport) =>
        new(
            canExport,
            canExport,
            canExport ? ChecksumValidationStatus.Valid : ChecksumValidationStatus.NotSupported,
            canExport ? "dummy-checksum" : "None",
            canExport ? ["Checksum OK"] : [],
            canExport ? [] : ["Checksum missing"]);

    private static CalibrationValidationResult CreateSafetyValidation(bool isAllowed) =>
        new(
            isAllowed,
            isAllowed ? CalibrationSafetyStatus.Safe : CalibrationSafetyStatus.Blocked,
            isAllowed ? ["Safe"] : [],
            [],
            isAllowed ? [] : ["Unsafe"],
            isAllowed ? [] : ["Unsafe"],
            []);
}
