namespace SafeEcu.Application.CalibrationPatching;

public sealed record CalibrationMapAddressRange(
    long StartOffset,
    int Length)
{
    public long EndOffset => StartOffset + Length - 1;
}
