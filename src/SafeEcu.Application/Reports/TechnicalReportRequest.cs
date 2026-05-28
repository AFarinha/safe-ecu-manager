namespace SafeEcu.Application.Reports;

public sealed record TechnicalReportRequest(Guid ComparisonId, string ReportsDirectory);
