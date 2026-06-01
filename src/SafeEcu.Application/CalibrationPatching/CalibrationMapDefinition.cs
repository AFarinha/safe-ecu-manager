namespace SafeEcu.Application.CalibrationPatching;

public sealed record CalibrationMapDefinition(
    string MapId,
    string DisplayName,
    string ParameterName,
    CalibrationMapAddressRange AddressRange,
    bool IsEmissionsRelated,
    IReadOnlyList<string> RequiredMapIds,
    string ProfileId = "dummy-test-profile",
    CalibrationMapDataType DataType = CalibrationMapDataType.UInt8,
    CalibrationMapEndianess Endianess = CalibrationMapEndianess.NotApplicable,
    decimal Factor = 1m,
    string Unit = "raw",
    decimal? MinimumValue = null,
    decimal? MaximumValue = null,
    CalibrationMapDefinitionSupportStatus SupportStatus = CalibrationMapDefinitionSupportStatus.Verified);
