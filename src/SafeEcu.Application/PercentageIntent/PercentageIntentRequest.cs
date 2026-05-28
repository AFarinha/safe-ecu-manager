namespace SafeEcu.Application.PercentageIntent;

public sealed record PercentageIntentRequest(
    Guid VehicleId,
    Guid EcuInfoId,
    string CalibrationProfileKey,
    decimal RequestedPercentage);
