using SafeEcu.Application.CalibrationProfiles;

namespace SafeEcu.Application.GuidedProfiles;

public sealed record GuidedCalibrationProfile(
    string Key,
    string Name,
    string UserDescription,
    IReadOnlyList<string> DoesNotDo,
    IReadOnlyList<string> Risks,
    IReadOnlyList<string> Requirements,
    IReadOnlyList<string> InternalTechnicalAreas,
    CalibrationProfileSupportStatus SupportStatus);
