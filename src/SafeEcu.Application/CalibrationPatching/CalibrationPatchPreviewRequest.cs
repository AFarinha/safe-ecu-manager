using SafeEcu.Application.Safety;
using SafeEcu.Domain.Calibrations;

namespace SafeEcu.Application.CalibrationPatching;

public sealed record CalibrationPatchPreviewRequest(
    string OriginalFilePath,
    string OriginalSha256Hash,
    string ProfileKey,
    CalibrationPatchMode Mode,
    IReadOnlyList<CalibrationMapDefinition> MapDefinitions,
    IReadOnlyList<CalibrationChange> Changes,
    IReadOnlyList<SafetyParameterProposal> SafetyProposals,
    IReadOnlyList<SafetyLimit> SafetyLimits,
    bool HasOriginalBackup,
    bool IsChecksumSupported,
    bool AreDependenciesSatisfied);
