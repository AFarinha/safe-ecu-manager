namespace SafeEcu.Application.Calibrations;

public sealed record BinaryComparisonRequest(Guid OriginalFileId, Guid ModifiedFileId);
