namespace SafeEcu.Domain.Projects;

public sealed class EcuProjectMapDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProjectId { get; set; }

    public EcuProject? Project { get; set; }

    public string MapId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string ParameterName { get; set; } = string.Empty;

    public string Category { get; set; } = "Unknown";

    public long StartOffset { get; set; }

    public int Length { get; set; }

    public int? RowCount { get; set; }

    public int? ColumnCount { get; set; }

    public string DataType { get; set; } = "Unknown";

    public string Endianess { get; set; } = "NotApplicable";

    public decimal Factor { get; set; } = 1m;

    public decimal OffsetCorrection { get; set; }

    public string Unit { get; set; } = "raw";

    public long? XAxisSourceOffset { get; set; }

    public int? XAxisLength { get; set; }

    public string? XAxisUnit { get; set; }

    public long? YAxisSourceOffset { get; set; }

    public int? YAxisLength { get; set; }

    public string? YAxisUnit { get; set; }

    public decimal? MinimumValue { get; set; }

    public decimal? MaximumValue { get; set; }

    public bool IsEmissionsRelated { get; set; }

    public string RequiredMapIds { get; set; } = string.Empty;

    public string ProfileId { get; set; } = string.Empty;

    public string SupportStatus { get; set; } = "Unknown";

    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
