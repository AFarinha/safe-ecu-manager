using SafeEcu.Application.CalibrationProfiles;
using SafeEcu.Application.GuidedProfiles;
using SafeEcu.Application.PercentageIntent;
using SafeEcu.Application.Programmers;
using SafeEcu.Application.Safety;
using SafeEcu.Programmers;

namespace SafeEcu.Tests.Security;

public sealed class SafetyRegressionTests
{
    [Fact]
    public void Default_programmer_catalog_has_no_direct_hardware_read_or_write()
    {
        var capabilities = new ProgrammerCapabilityService(ProgrammerAdapterCatalog.CreateDefaultAdapters())
            .GetCapabilities();

        Assert.NotEmpty(capabilities);
        Assert.All(capabilities, capability => Assert.False(capability.DirectReadSupported));
        Assert.All(capabilities, capability => Assert.False(capability.DirectWriteSupported));
    }

    [Fact]
    public void Guided_profiles_do_not_enable_real_calibration_workflows()
    {
        var profiles = new GuidedCalibrationProfileCatalog().ListAll();
        var realCalibrationProfiles = profiles.Where(profile =>
            profile.Key is "eco-conservative"
                or "smooth-response"
                or "conservative-torque"
                or "stage-1-conservative");

        Assert.NotEmpty(realCalibrationProfiles);
        Assert.All(
            realCalibrationProfiles,
            profile => Assert.Equal(CalibrationProfileSupportStatus.NotSupported, profile.SupportStatus));
    }

    [Fact]
    public void Percentage_intent_results_never_create_modified_files_in_current_phase()
    {
        var result = new PercentageIntentResult(
            false,
            new CalibrationChangeSet(
                "conservative-torque",
                5m,
                ["Blocked before technical map conversion."],
                CreatesModifiedFile: false),
            new(false, CalibrationSafetyStatus.Blocked, [], [], ["Blocked"], ["Blocked"], []),
            new([], ["Blocked"]));

        Assert.False(result.ChangeSet.CreatesModifiedFile);
    }
}
