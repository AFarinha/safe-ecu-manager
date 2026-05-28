namespace SafeEcu.Application.Checksums;

public sealed record ChecksumValidationResult(
    ChecksumValidationStatus Status,
    string AlgorithmName,
    string Message)
{
    public bool IsSupported => Status != ChecksumValidationStatus.NotSupported;
}
