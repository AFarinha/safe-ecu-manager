namespace SafeEcu.Calibration;

public sealed record BinaryDifferenceBlock(
    long StartOffset,
    long EndOffset,
    long DifferenceCount)
{
    public long Length => EndOffset - StartOffset + 1;
}
