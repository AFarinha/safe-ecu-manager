namespace SafeEcu.Application.Checksums;

public sealed record EcuSoftwareChecksumSupportResult(
    bool IsSupported,
    bool CanExportCalibrationOutput,
    ChecksumValidationStatus Status,
    string AlgorithmId,
    IReadOnlyList<string> Messages,
    IReadOnlyList<string> BlockReasons);
