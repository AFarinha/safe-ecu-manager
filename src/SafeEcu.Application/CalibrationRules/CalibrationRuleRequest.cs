using SafeEcu.Application.ControlledParameters;
using SafeEcu.Application.Safety;
using SafeEcu.Domain.Calibrations;

namespace SafeEcu.Application.CalibrationRules;

public sealed record CalibrationRuleRequest(
    string ProfileKey,
    IReadOnlyList<string> ParameterNames,
    IReadOnlyList<SafetyParameterProposal> SafetyProposals,
    IReadOnlyList<SafetyLimit> SafetyLimits,
    bool HasOriginalBackup,
    bool IsChecksumSupported,
    bool AreMapsIdentified);
