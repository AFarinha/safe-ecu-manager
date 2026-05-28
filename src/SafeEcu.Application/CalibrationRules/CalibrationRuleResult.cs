using SafeEcu.Application.PercentageIntent;
using SafeEcu.Application.Safety;

namespace SafeEcu.Application.CalibrationRules;

public sealed record CalibrationRuleResult(
    bool IsAllowed,
    CalibrationChangeSet ChangeSet,
    CalibrationValidationResult ValidationResult,
    CalibrationSafetyReport SafetyReport);
