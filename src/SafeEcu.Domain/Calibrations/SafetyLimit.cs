namespace SafeEcu.Domain.Calibrations;

public sealed class SafetyLimit
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string EcuFamily { get; set; } = string.Empty;

    public string SoftwareVersion { get; set; } = string.Empty;

    public string EngineCode { get; set; } = string.Empty;

    public string ParameterName { get; set; } = string.Empty;

    public decimal MinValue { get; set; }

    public decimal MaxValue { get; set; }

    public string Unit { get; set; } = string.Empty;

    public string Severity { get; set; } = "Blocked";

    public string Reason { get; set; } = string.Empty;

    public string SafetyStatus { get; set; } = "Conservative";

    public string Source { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
