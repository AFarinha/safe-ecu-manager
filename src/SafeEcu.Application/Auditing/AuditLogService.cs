using SafeEcu.Application.Common;
using SafeEcu.Application.Persistence;
using SafeEcu.Domain.Auditing;

namespace SafeEcu.Application.Auditing;

public sealed class AuditLogService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IAppLogger _logger;

    public AuditLogService(IAuditLogRepository auditLogRepository, IAppLogger logger)
    {
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    public async Task<OperationResult<AuditLogEntry>> RecordAsync(
        AuditLogRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Action))
        {
            return OperationResult<AuditLogEntry>.Failure("Audit action is required.");
        }

        if (string.IsNullOrWhiteSpace(request.EntityType))
        {
            return OperationResult<AuditLogEntry>.Failure("Audit entity type is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return OperationResult<AuditLogEntry>.Failure("Audit message is required.");
        }

        var entry = new AuditLogEntry
        {
            Action = request.Action.Trim(),
            EntityType = request.EntityType.Trim(),
            EntityId = request.EntityId.Trim(),
            Severity = string.IsNullOrWhiteSpace(request.Severity) ? "Information" : request.Severity.Trim(),
            Message = request.Message.Trim(),
            Details = request.Details?.Trim() ?? string.Empty,
            UserOrMachineName = string.IsNullOrWhiteSpace(request.UserOrMachineName)
                ? Environment.MachineName
                : request.UserOrMachineName.Trim(),
            CorrelationId = request.CorrelationId ?? Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow
        };

        await _auditLogRepository.AddAsync(entry, cancellationToken);
        _logger.Information($"Audit entry recorded: {entry.Action} {entry.EntityType} {entry.EntityId}.");

        return OperationResult<AuditLogEntry>.Success(entry);
    }

    public Task<IReadOnlyList<AuditLogEntry>> ListRecentAsync(
        int take = 100,
        CancellationToken cancellationToken = default) =>
        _auditLogRepository.ListRecentAsync(take, cancellationToken);
}
