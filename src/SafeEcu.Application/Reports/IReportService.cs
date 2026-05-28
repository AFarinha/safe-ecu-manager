using SafeEcu.Application.Common;

namespace SafeEcu.Application.Reports;

public interface IReportService
{
    Task<OperationResult<string>> GenerateTechnicalReportAsync(
        TechnicalReportRequest request,
        CancellationToken cancellationToken = default);
}
