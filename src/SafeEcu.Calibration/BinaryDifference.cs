namespace SafeEcu.Calibration;

public sealed record BinaryDifference(
    long Offset,
    byte? OriginalByte,
    byte? ModifiedByte);
