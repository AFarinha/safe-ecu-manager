using SafeEcu.Domain.Vehicles;

namespace SafeEcu.Domain.Calibrations;

public sealed class CalibrationComparison
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OriginalFileId { get; set; }

    public EcuFile? OriginalFile { get; set; }

    public Guid ModifiedFileId { get; set; }

    public EcuFile? ModifiedFile { get; set; }

    public long DifferenceCount { get; set; }

    public decimal PercentChanged { get; set; }

    public string Result { get; set; } = string.Empty;

    public string DifferenceSummary { get; set; } = string.Empty;

    public string DifferenceBlockSummary { get; set; } = string.Empty;

    public DateTimeOffset ComparedAt { get; set; } = DateTimeOffset.UtcNow;
}
