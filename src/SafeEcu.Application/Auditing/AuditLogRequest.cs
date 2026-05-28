namespace SafeEcu.Application.Auditing;

public sealed record AuditLogRequest(
    string Action,
    string EntityType,
    string EntityId,
    string Severity,
    string Message,
    string? Details = null,
    string? UserOrMachineName = null,
    Guid? CorrelationId = null);
