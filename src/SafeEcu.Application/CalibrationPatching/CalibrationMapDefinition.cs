namespace SafeEcu.Application.CalibrationPatching;

public sealed record CalibrationMapDefinition(
    string MapId,
    string DisplayName,
    string ParameterName,
    CalibrationMapAddressRange AddressRange,
    bool IsEmissionsRelated,
    IReadOnlyList<string> RequiredMapIds);
