namespace SafeEcu.Application.CalibrationProfiles;

public sealed class CalibrationProfileCatalog
{
    private static readonly IReadOnlyList<CalibrationProfileDefinition> Profiles =
    [
        new(
            "diagnostic-only",
            "Diagnostic only",
            "Allows documentation, comparison and reporting without calibration changes.",
            0m,
            0m,
            CalibrationProfileSupportStatus.Supported,
            "No ECU data is modified."),
        new(
            "eco-conservative",
            "Eco conservative",
            "Future profile placeholder for conservative economy-oriented calibration intent.",
            -10m,
            0m,
            CalibrationProfileSupportStatus.NotSupported,
            "Technical conversion rules are not implemented for any ECU yet."),
        new(
            "smooth-response",
            "Smooth response",
            "Future profile placeholder for gentle drivability changes.",
            0m,
            3m,
            CalibrationProfileSupportStatus.NotSupported,
            "Map identification and checksum support are required before use."),
        new(
            "conservative-torque",
            "Conservative torque",
            "Future profile placeholder for conservative torque intent.",
            0m,
            5m,
            CalibrationProfileSupportStatus.NotSupported,
            "No real calibration changes are generated in this phase."),
        new(
            "supported-conservative",
            "Supported conservative",
            "Future profile placeholder that requires ECU-specific support.",
            0m,
            8m,
            CalibrationProfileSupportStatus.NotSupported,
            "Blocked until ECU, software version, maps and checksum are supported."),
        new(
            "stage-1-conservative",
            "Stage 1 conservative",
            "Future profile placeholder that requires verified ECU-specific support.",
            0m,
            0m,
            CalibrationProfileSupportStatus.NotSupported,
            "Blocked until the exact ECU/software combination is verified.")
    ];

    public IReadOnlyList<CalibrationProfileDefinition> ListAll() => Profiles;

    public CalibrationProfileDefinition? FindByKey(string profileKey) =>
        Profiles.SingleOrDefault(profile =>
            string.Equals(profile.Key, profileKey, StringComparison.OrdinalIgnoreCase));
}
