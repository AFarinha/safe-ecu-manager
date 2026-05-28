namespace SafeEcu.Application.CalibrationProfiles;

public sealed class ProfilePercentageLimitService
{
    private readonly ProfilePercentageLimitCatalog _catalog;

    public ProfilePercentageLimitService(ProfilePercentageLimitCatalog catalog)
    {
        _catalog = catalog;
    }

    public ProfilePercentageLimitResult Validate(ProfilePercentageLimitRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ProfileKey)
            || string.IsNullOrWhiteSpace(request.EcuFamily)
            || string.IsNullOrWhiteSpace(request.EngineCode)
            || string.IsNullOrWhiteSpace(request.SoftwareVersion))
        {
            return Block(request, null, "Profile, ECU family, engine code and software version are required.");
        }

        var limit = _catalog.FindSpecific(request);
        if (limit is null)
        {
            return Block(
                request,
                null,
                "No ECU/motor/software-specific percentage limit is defined. Operation blocked.");
        }

        if (request.RequestedPercentage < limit.MinimumPercentage
            || request.RequestedPercentage > limit.MaximumPercentage)
        {
            return Block(
                request,
                limit,
                $"Requested percentage must be between {limit.MinimumPercentage}% and {limit.MaximumPercentage}%.");
        }

        return new ProfilePercentageLimitResult(
            true,
            request.RequestedPercentage,
            limit.MinimumPercentage,
            limit.MaximumPercentage,
            [$"Requested percentage is within the configured limit for '{request.ProfileKey}'."],
            []);
    }

    private static ProfilePercentageLimitResult Block(
        ProfilePercentageLimitRequest request,
        ProfilePercentageLimit? limit,
        string reason) =>
        new(
            false,
            request.RequestedPercentage,
            limit?.MinimumPercentage,
            limit?.MaximumPercentage,
            [],
            [reason]);
}
