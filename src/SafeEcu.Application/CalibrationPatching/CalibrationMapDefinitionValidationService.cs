using SafeEcu.Application.EcuProfiles;

namespace SafeEcu.Application.CalibrationPatching;

public sealed class CalibrationMapDefinitionValidationService
{
    public CalibrationMapDefinitionValidationResult Validate(CalibrationMapDefinitionValidationRequest request)
    {
        var messages = new List<string>();
        var blockReasons = new List<string>();

        if (!IsDummyOrTestProfile(request.Profile))
        {
            blockReasons.Add("Only dummy/test ECU software profiles can define maps in this phase.");
        }

        if (request.Profile.SupportStatus != VerifiedEcuSoftwareSupportStatus.Verified)
        {
            blockReasons.Add("Map definitions require a verified ECU software profile.");
        }

        if (request.MapDefinitions.Count == 0)
        {
            blockReasons.Add("At least one map definition is required.");
        }

        foreach (var map in request.MapDefinitions)
        {
            ValidateMap(request.Profile, map, blockReasons);
        }

        if (blockReasons.Count == 0)
        {
            messages.Add("Calibration map definitions are valid for dummy/test preview use.");
        }

        return new CalibrationMapDefinitionValidationResult(blockReasons.Count == 0, messages, blockReasons);
    }

    private static void ValidateMap(
        VerifiedEcuSoftwareProfile profile,
        CalibrationMapDefinition map,
        ICollection<string> blockReasons)
    {
        if (string.IsNullOrWhiteSpace(map.MapId))
        {
            blockReasons.Add("Map id is required.");
        }

        if (!profile.AllowedMapIds.Contains(map.MapId, StringComparer.OrdinalIgnoreCase))
        {
            blockReasons.Add($"Map '{map.MapId}' is not allowed by the ECU software profile.");
        }

        if (!string.Equals(map.ProfileId, profile.ProfileId, StringComparison.OrdinalIgnoreCase))
        {
            blockReasons.Add($"Map '{map.MapId}' does not belong to profile '{profile.ProfileId}'.");
        }

        if (map.SupportStatus != CalibrationMapDefinitionSupportStatus.Verified)
        {
            blockReasons.Add($"Map '{map.MapId}' is {map.SupportStatus}; verified map definition is required.");
        }

        if (map.AddressRange.StartOffset < 0 || map.AddressRange.Length <= 0)
        {
            blockReasons.Add($"Map '{map.MapId}' has an invalid address range.");
        }

        if (map.DataType == CalibrationMapDataType.Unknown)
        {
            blockReasons.Add($"Map '{map.MapId}' has an unknown data type.");
        }

        if (map.Factor <= 0)
        {
            blockReasons.Add($"Map '{map.MapId}' must define a positive conversion factor.");
        }

        if (map.MinimumValue is not null && map.MaximumValue is not null && map.MinimumValue > map.MaximumValue)
        {
            blockReasons.Add($"Map '{map.MapId}' has invalid min/max limits.");
        }

        if (map.IsEmissionsRelated)
        {
            blockReasons.Add($"Map '{map.MapId}' is emissions-related and cannot be used for calibration changes.");
        }
    }

    private static bool IsDummyOrTestProfile(VerifiedEcuSoftwareProfile profile) =>
        profile.ProfileId.Contains("dummy", StringComparison.OrdinalIgnoreCase)
        || profile.ProfileId.Contains("test", StringComparison.OrdinalIgnoreCase)
        || profile.Source.Contains("unit test", StringComparison.OrdinalIgnoreCase);
}
