namespace SafeEcu.Application.Safety;

public sealed record CalibrationParameterCheck(
    string ParameterName,
    decimal CurrentValue,
    decimal ProposedValue,
    decimal? MinAllowed,
    decimal? MaxAllowed,
    string Unit,
    CalibrationSafetyStatus Status,
    string Reason);
