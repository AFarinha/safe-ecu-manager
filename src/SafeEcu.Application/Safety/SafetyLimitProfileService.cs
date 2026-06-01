using SafeEcu.Application.Persistence;

namespace SafeEcu.Application.Safety;

public sealed class SafetyLimitProfileService
{
    private readonly ISafetyLimitRepository _safetyLimitRepository;
    private readonly SafetyLimitEngine _safetyLimitEngine;

    public SafetyLimitProfileService(
        ISafetyLimitRepository safetyLimitRepository,
        SafetyLimitEngine safetyLimitEngine)
    {
        _safetyLimitRepository = safetyLimitRepository;
        _safetyLimitEngine = safetyLimitEngine;
    }

    public async Task<CalibrationValidationResult> ValidateAsync(
        SafetyLimitProfileValidationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ProfileId))
        {
            return Block("Profile id is required for safety limit validation.");
        }

        var limits = await _safetyLimitRepository.ListByProfileAsync(request.ProfileId, cancellationToken);
        if (limits.Count == 0)
        {
            return Block($"No conservative safety limits are persisted for profile '{request.ProfileId}'.");
        }

        return _safetyLimitEngine.Validate(request.Proposals, limits);
    }

    private static CalibrationValidationResult Block(string reason) =>
        new(
            false,
            CalibrationSafetyStatus.Unknown,
            [],
            [],
            [reason],
            [reason],
            []);
}
