using SafeEcu.Application.ControlledParameters;
using SafeEcu.Application.GuidedProfiles;
using SafeEcu.Application.PercentageIntent;
using SafeEcu.Application.Safety;

namespace SafeEcu.Application.CalibrationRules;

public sealed class CalibrationRuleEngine
{
    private readonly GuidedProfileService _guidedProfileService;
    private readonly ControlledParameterRuleEngine _controlledParameterRuleEngine;
    private readonly SafetyLimitEngine _safetyLimitEngine;

    public CalibrationRuleEngine(
        GuidedProfileService guidedProfileService,
        ControlledParameterRuleEngine controlledParameterRuleEngine,
        SafetyLimitEngine safetyLimitEngine)
    {
        _guidedProfileService = guidedProfileService;
        _controlledParameterRuleEngine = controlledParameterRuleEngine;
        _safetyLimitEngine = safetyLimitEngine;
    }

    public CalibrationRuleResult Evaluate(CalibrationRuleRequest request)
    {
        var messages = new List<string>();
        var warnings = new List<string>();
        var blockReasons = new List<string>();

        var profileAvailability = _guidedProfileService.EvaluateAvailability(request.ProfileKey);
        messages.AddRange(profileAvailability.Messages);
        blockReasons.AddRange(profileAvailability.BlockReasons);

        if (!request.HasOriginalBackup)
        {
            blockReasons.Add("Original backup is required before calibration rule evaluation.");
        }

        if (!request.AreMapsIdentified)
        {
            blockReasons.Add("Required maps are not identified with sufficient confidence.");
        }

        if (!request.IsChecksumSupported)
        {
            blockReasons.Add("Checksum support is required before generating calibration output.");
        }

        var controlledParameters = _controlledParameterRuleEngine.Validate(
            new ControlledParameterProposal(request.ProfileKey, request.ParameterNames));
        messages.AddRange(controlledParameters.Messages);
        blockReasons.AddRange(controlledParameters.BlockReasons);

        var safetyValidation = _safetyLimitEngine.Validate(request.SafetyProposals, request.SafetyLimits);
        messages.AddRange(safetyValidation.Messages);
        warnings.AddRange(safetyValidation.Warnings);
        blockReasons.AddRange(safetyValidation.BlockReasons);

        var isAllowed = blockReasons.Count == 0;
        var changeSet = new CalibrationChangeSet(
            request.ProfileKey,
            0m,
            isAllowed
                ? ["Validated rule set only. No ECU writing is performed."]
                : ["Blocked before calibration output generation."],
            CreatesModifiedFile: false);

        var validationResult = new CalibrationValidationResult(
            isAllowed,
            isAllowed
                ? (warnings.Count > 0 ? CalibrationSafetyStatus.Warning : CalibrationSafetyStatus.Safe)
                : CalibrationSafetyStatus.Blocked,
            messages,
            warnings,
            blockReasons,
            blockReasons,
            safetyValidation.CheckedParameters);

        return new CalibrationRuleResult(
            isAllowed,
            changeSet,
            validationResult,
            new CalibrationSafetyReport(messages, warnings, blockReasons));
    }
}
