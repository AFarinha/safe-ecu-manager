using SafeEcu.Application.CalibrationProfiles;

namespace SafeEcu.Application.GuidedProfiles;

public sealed class GuidedCalibrationProfileCatalog
{
    private static readonly IReadOnlyList<GuidedCalibrationProfile> Profiles =
    [
        new(
            "original-restore",
            "Original / Restore factory",
            "Keeps or restores documented original calibration data when a verified original backup is available.",
            ["Does not write to the ECU in this application phase.", "Does not bypass immobilizer, tuning protection or security access."],
            ["Wrong original file selection can be mechanically and legally risky."],
            ["Verified original backup", "Confirmed vehicle", "Confirmed ECU/EDU identity"],
            ["Original file tracking", "Hash verification", "Report generation"],
            CalibrationProfileSupportStatus.NotSupported),
        new(
            "diagnostic-only",
            "Diagnostic only",
            "Allows safe documentation, file management, comparison and reporting.",
            ["Does not modify maps.", "Does not create a modified ECU file.", "Does not write to the ECU."],
            ["Incomplete vehicle or ECU data can reduce report confidence."],
            ["Vehicle record", "Manual ECU/EDU identification when available"],
            ["File management", "Binary comparison", "Technical reporting"],
            CalibrationProfileSupportStatus.Supported),
        new(
            "technical-comparison",
            "Technical comparison",
            "Compares original and modified files as binary data without interpreting calibration maps.",
            ["Does not decide whether a calibration is safe.", "Does not validate real map logic."],
            ["Binary differences alone do not prove calibration quality or safety."],
            ["Original file", "Modified file", "Same vehicle", "Same confirmed ECU/EDU"],
            ["Offset scanning", "Difference grouping", "CSV export"],
            CalibrationProfileSupportStatus.Supported),
        new(
            "eco-conservative",
            "Eco conservative",
            "Future guided profile for conservative economy-oriented intent.",
            ["Does not disable emissions systems.", "Does not apply blind percentage changes."],
            ["Fuel, air, torque and emissions dependencies must be validated first."],
            ["Verified ECU family", "Supported map definitions", "Checksum support", "Safety limits"],
            ["Fuel quantity", "Torque limiters", "Smoke limiter", "Driver request"],
            CalibrationProfileSupportStatus.NotSupported),
        new(
            "smooth-response",
            "Smooth response",
            "Future guided profile for gentle response changes.",
            ["Does not edit pedal maps freely.", "Does not change isolated parameters."],
            ["Poor dependency handling can affect drivability and reliability."],
            ["Verified ECU/software", "Supported map definitions", "Safety limits"],
            ["Driver request", "Torque model", "Smoke limiter"],
            CalibrationProfileSupportStatus.NotSupported),
        new(
            "conservative-torque",
            "Conservative torque",
            "Future guided profile for controlled torque intent.",
            ["Does not increase injection in isolation.", "Does not bypass clutch, gearbox or emissions limits."],
            ["Torque changes can affect clutch, gearbox, turbo, injectors, EGT and emissions."],
            ["Verified ECU/software", "Supported maps", "Checksum support", "Original backup", "Safety report"],
            ["Torque limiters", "Fuel quantity", "Rail pressure", "Turbo pressure", "Smoke limiter"],
            CalibrationProfileSupportStatus.NotSupported),
        new(
            "stage-1-conservative",
            "Stage 1 conservative",
            "Future profile only for exact verified ECU/software combinations.",
            ["Does not promise power increase.", "Does not run unless all technical support is verified."],
            ["Higher mechanical, thermal, emissions and legal risk than diagnostic workflows."],
            ["Verified ECU/software", "Professional validation", "Checksum support", "Conservative safety limits"],
            ["Full supported calibration rule set"],
            CalibrationProfileSupportStatus.NotSupported)
    ];

    public IReadOnlyList<GuidedCalibrationProfile> ListAll() => Profiles;

    public GuidedCalibrationProfile? FindByKey(string profileKey) =>
        Profiles.SingleOrDefault(profile =>
            string.Equals(profile.Key, profileKey, StringComparison.OrdinalIgnoreCase));
}
