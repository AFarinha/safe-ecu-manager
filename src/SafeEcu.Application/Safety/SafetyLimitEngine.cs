using SafeEcu.Domain.Calibrations;

namespace SafeEcu.Application.Safety;

public sealed class SafetyLimitEngine
{
    private const decimal WarningDistanceRatio = 0.10m;

    public CalibrationValidationResult Validate(
        IEnumerable<SafetyParameterProposal> proposals,
        IEnumerable<SafetyLimit> limits)
    {
        var limitLookup = limits
            .Where(limit => !string.IsNullOrWhiteSpace(limit.ParameterName))
            .GroupBy(limit => Normalize(limit.ParameterName))
            .ToDictionary(group => group.Key, group => group.First());

        var messages = new List<string>();
        var warnings = new List<string>();
        var errors = new List<string>();
        var blockReasons = new List<string>();
        var checks = new List<CalibrationParameterCheck>();

        foreach (var proposal in proposals)
        {
            var parameterName = proposal.ParameterName.Trim();
            if (string.IsNullOrWhiteSpace(parameterName))
            {
                AddBlockedCheck(
                    checks,
                    "Unknown",
                    proposal.CurrentValue,
                    proposal.ProposedValue,
                    null,
                    null,
                    string.Empty,
                    "Parameter name is required.");
                blockReasons.Add("Parameter name is required.");
                continue;
            }

            if (!limitLookup.TryGetValue(Normalize(parameterName), out var limit))
            {
                var reason = $"No conservative safety limit is defined for parameter '{parameterName}'. Operation blocked.";
                AddBlockedCheck(
                    checks,
                    parameterName,
                    proposal.CurrentValue,
                    proposal.ProposedValue,
                    null,
                    null,
                    string.Empty,
                    reason);
                blockReasons.Add(reason);
                continue;
            }

            if (proposal.ProposedValue < limit.MinValue || proposal.ProposedValue > limit.MaxValue)
            {
                var reason = string.IsNullOrWhiteSpace(limit.Reason)
                    ? $"Proposed value for '{parameterName}' is outside the conservative allowed range."
                    : limit.Reason;
                AddBlockedCheck(
                    checks,
                    parameterName,
                    proposal.CurrentValue,
                    proposal.ProposedValue,
                    limit.MinValue,
                    limit.MaxValue,
                    limit.Unit,
                    reason);
                blockReasons.Add(reason);
                continue;
            }

            var status = IsNearLimit(proposal.ProposedValue, limit)
                ? CalibrationSafetyStatus.Warning
                : CalibrationSafetyStatus.Safe;
            var check = new CalibrationParameterCheck(
                parameterName,
                proposal.CurrentValue,
                proposal.ProposedValue,
                limit.MinValue,
                limit.MaxValue,
                limit.Unit,
                status,
                status == CalibrationSafetyStatus.Warning
                    ? $"Proposed value for '{parameterName}' is close to a conservative limit."
                    : $"Proposed value for '{parameterName}' is inside the conservative allowed range.");

            checks.Add(check);
            if (status == CalibrationSafetyStatus.Warning)
            {
                warnings.Add(check.Reason);
            }
            else
            {
                messages.Add(check.Reason);
            }
        }

        if (checks.Count == 0)
        {
            const string reason = "No parameters were supplied for safety validation. Operation blocked.";
            errors.Add(reason);
            blockReasons.Add(reason);
        }

        errors.AddRange(blockReasons);
        var overallStatus = DetermineOverallStatus(checks, blockReasons);

        return new CalibrationValidationResult(
            IsAllowed: overallStatus is CalibrationSafetyStatus.Safe or CalibrationSafetyStatus.Warning,
            overallStatus,
            messages,
            warnings,
            errors,
            blockReasons,
            checks);
    }

    private static CalibrationSafetyStatus DetermineOverallStatus(
        IReadOnlyList<CalibrationParameterCheck> checks,
        IReadOnlyList<string> blockReasons)
    {
        if (blockReasons.Count > 0)
        {
            return checks.Any(check => check.Status == CalibrationSafetyStatus.Unknown)
                ? CalibrationSafetyStatus.Unknown
                : CalibrationSafetyStatus.Blocked;
        }

        return checks.Any(check => check.Status == CalibrationSafetyStatus.Warning)
            ? CalibrationSafetyStatus.Warning
            : CalibrationSafetyStatus.Safe;
    }

    private static void AddBlockedCheck(
        ICollection<CalibrationParameterCheck> checks,
        string parameterName,
        decimal currentValue,
        decimal proposedValue,
        decimal? minAllowed,
        decimal? maxAllowed,
        string unit,
        string reason) =>
        checks.Add(new CalibrationParameterCheck(
            parameterName,
            currentValue,
            proposedValue,
            minAllowed,
            maxAllowed,
            unit,
            minAllowed is null || maxAllowed is null
                ? CalibrationSafetyStatus.Unknown
                : CalibrationSafetyStatus.Blocked,
            reason));

    private static bool IsNearLimit(decimal proposedValue, SafetyLimit limit)
    {
        var range = limit.MaxValue - limit.MinValue;
        if (range <= 0)
        {
            return true;
        }

        var warningDistance = range * WarningDistanceRatio;
        return proposedValue - limit.MinValue <= warningDistance
            || limit.MaxValue - proposedValue <= warningDistance;
    }

    private static string Normalize(string value) =>
        value.Trim().ToUpperInvariant();
}
