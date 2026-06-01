using SafeEcu.Application.GuidedProfiles;
using SafeEcu.Application.Safety;

namespace SafeEcu.Application.CalibrationPatching;

public sealed class CalibrationPatchValidationService
{
    private readonly GuidedProfileService _guidedProfileService;
    private readonly SafetyLimitEngine _safetyLimitEngine;

    public CalibrationPatchValidationService(
        GuidedProfileService guidedProfileService,
        SafetyLimitEngine safetyLimitEngine)
    {
        _guidedProfileService = guidedProfileService;
        _safetyLimitEngine = safetyLimitEngine;
    }

    public IReadOnlyList<string> Validate(CalibrationPatchPreviewRequest request)
    {
        var blockReasons = new List<string>();

        if (!File.Exists(request.OriginalFilePath))
        {
            blockReasons.Add("Original ECU file does not exist.");
        }

        if (string.IsNullOrWhiteSpace(request.OriginalSha256Hash))
        {
            blockReasons.Add("Original SHA-256 hash is required.");
        }

        if (!request.HasOriginalBackup)
        {
            blockReasons.Add("Verified original backup is required.");
        }

        if (!request.IsChecksumSupported)
        {
            blockReasons.Add("Checksum support is required before generating or previewing a calibration patch.");
        }

        if (!request.AreDependenciesSatisfied)
        {
            blockReasons.Add("Calibration map dependencies are not satisfied.");
        }

        if (request.Mode is not CalibrationPatchMode.Preview and not CalibrationPatchMode.DummyPatch)
        {
            blockReasons.Add("Only Preview and DummyPatch modes are supported.");
        }

        var profileAvailability = _guidedProfileService.EvaluateAvailability(request.ProfileKey);
        blockReasons.AddRange(profileAvailability.BlockReasons);

        var definitionsById = request.MapDefinitions.ToDictionary(
            definition => definition.MapId,
            StringComparer.OrdinalIgnoreCase);

        foreach (var change in request.Changes)
        {
            if (!definitionsById.TryGetValue(change.MapId, out var definition))
            {
                blockReasons.Add($"Map '{change.MapId}' is not defined for this ECU/software profile.");
                continue;
            }

            if (definition.IsEmissionsRelated)
            {
                blockReasons.Add($"Map '{definition.DisplayName}' is emissions-related and cannot be modified.");
            }

            var missingDependencies = definition.RequiredMapIds
                .Where(requiredMapId => !request.Changes.Any(change => string.Equals(change.MapId, requiredMapId, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (missingDependencies.Count > 0)
            {
                blockReasons.Add(
                    $"Map '{definition.DisplayName}' is missing required map changes: {string.Join(", ", missingDependencies)}.");
            }
        }

        var safety = _safetyLimitEngine.Validate(request.SafetyProposals, request.SafetyLimits);
        blockReasons.AddRange(safety.BlockReasons);

        return blockReasons;
    }
}
