namespace SafeEcu.Calibration;

public sealed record BinaryDifferenceScanOptions(
    int MaxSampledDifferences = 500,
    int MaxBlocks = 200)
{
    public static BinaryDifferenceScanOptions Default { get; } = new();
}
