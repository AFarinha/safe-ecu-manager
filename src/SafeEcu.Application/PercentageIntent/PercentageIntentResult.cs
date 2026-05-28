using SafeEcu.Application.Safety;

namespace SafeEcu.Application.PercentageIntent;

public sealed record PercentageIntentResult(
    bool IsAllowed,
    CalibrationChangeSet ChangeSet,
    CalibrationValidationResult ValidationResult,
    SafetyReport SafetyReport);
