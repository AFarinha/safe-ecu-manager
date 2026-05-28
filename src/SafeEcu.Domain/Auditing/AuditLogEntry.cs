namespace SafeEcu.Domain.Auditing;

public sealed class AuditLogEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string EntityId { get; set; } = string.Empty;

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    public string Severity { get; set; } = "Information";

    public string Message { get; set; } = string.Empty;

    public string Details { get; set; } = string.Empty;

    public string UserOrMachineName { get; set; } = Environment.MachineName;

    public Guid CorrelationId { get; set; } = Guid.NewGuid();
}
