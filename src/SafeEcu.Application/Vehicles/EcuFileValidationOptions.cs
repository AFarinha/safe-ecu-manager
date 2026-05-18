namespace SafeEcu.Application.Vehicles;

public sealed class EcuFileValidationOptions
{
    public long MinimumSizeBytes { get; init; } = 1;

    public long MaximumSizeBytes { get; init; } = 16L * 1024L * 1024L;

    public IReadOnlySet<string> AllowedExtensions { get; init; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".bin",
            ".ori",
            ".mod"
        };
}
