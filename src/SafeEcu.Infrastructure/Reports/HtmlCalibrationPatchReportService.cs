using System.Net;
using System.Text;
using SafeEcu.Application.Common;
using SafeEcu.Application.Reports;

namespace SafeEcu.Infrastructure.Reports;

public sealed class HtmlCalibrationPatchReportService
{
    private readonly IAppLogger _logger;

    public HtmlCalibrationPatchReportService(IAppLogger logger)
    {
        _logger = logger;
    }

    public async Task<OperationResult<string>> GenerateAsync(
        CalibrationPatchReportRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ReportsDirectory))
        {
            return OperationResult<string>.Failure("Reports directory is required.");
        }

        Directory.CreateDirectory(request.ReportsDirectory);
        var reportPath = Path.Combine(
            request.ReportsDirectory,
            $"calibration-patch-report-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}.html");

        await File.WriteAllTextAsync(reportPath, BuildHtml(request), Encoding.UTF8, cancellationToken);
        _logger.Information($"Calibration patch HTML report generated: {reportPath}.");

        return OperationResult<string>.Success(reportPath);
    }

    private static string BuildHtml(CalibrationPatchReportRequest request)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\">");
        builder.AppendLine("<title>Safe ECU Calibration Patch Report</title>");
        builder.AppendLine("""
            <style>
            body { font-family: Segoe UI, Arial, sans-serif; margin: 32px; color: #17202A; }
            h1 { margin-bottom: 4px; }
            h2 { margin-top: 28px; border-bottom: 1px solid #D6DEE6; padding-bottom: 6px; }
            table { border-collapse: collapse; width: 100%; margin-top: 10px; }
            th, td { border: 1px solid #D6DEE6; padding: 8px; text-align: left; vertical-align: top; }
            th { background: #F4F6F8; }
            .warning { background: #FFF7E6; border: 1px solid #E2B96B; padding: 12px; margin-top: 18px; }
            .blocked { background: #FDEDEC; border: 1px solid #D98880; padding: 12px; margin-top: 18px; }
            .muted { color: #5F6C7B; }
            </style>
            """);
        builder.AppendLine("</head><body>");
        builder.AppendLine("<h1>Safe ECU Calibration Patch Report</h1>");
        builder.AppendLine($"<div class=\"muted\">Generated at {Html(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz"))}</div>");
        builder.AppendLine("<div class=\"warning\">This report does not approve ECU writing or flashing. Checksum valid does not mean calibration safe.</div>");

        AddSection(builder, "Vehicle", ("Summary", request.VehicleSummary));
        AddSection(builder, "ECU/Software Profile",
            ("Summary", request.EcuSummary),
            ("Profile id", request.Profile.ProfileId),
            ("ECU family", request.Profile.EcuFamily),
            ("Software version", request.Profile.SoftwareVersion),
            ("Support status", request.Profile.SupportStatus.ToString()));

        AddSection(builder, "Original File",
            ("Path", request.OriginalFilePath),
            ("SHA-256", request.OriginalSha256Hash));

        builder.AppendLine("<h2>Patch Preview</h2>");
        builder.AppendLine("<table><tr><th>Map</th><th>Parameter</th><th>Offset</th><th>Before</th><th>After</th><th>Reason</th></tr>");
        foreach (var item in request.PatchPreview.PreviewItems ?? [])
        {
            builder.Append("<tr>");
            AddCell(builder, item.DisplayName);
            AddCell(builder, item.ParameterName);
            AddCell(builder, item.AbsoluteOffset.ToString());
            AddCell(builder, $"0x{item.CurrentValue:X2}");
            AddCell(builder, $"0x{item.ProposedValue:X2}");
            AddCell(builder, item.Reason);
            builder.AppendLine("</tr>");
        }
        builder.AppendLine("</table>");

        AddSection(builder, "Safety",
            ("Allowed", request.SafetyValidation.IsAllowed.ToString()),
            ("Status", request.SafetyValidation.Status.ToString()),
            ("Block reasons", string.Join("; ", request.SafetyValidation.BlockReasons)));

        AddSection(builder, "Checksum",
            ("Supported", request.ChecksumSupport.IsSupported.ToString()),
            ("Algorithm", request.ChecksumSupport.AlgorithmId),
            ("Export allowed", request.ChecksumSupport.CanExportCalibrationOutput.ToString()));

        builder.AppendLine("<h2>Audit</h2><table><tr><th>Timestamp</th><th>Action</th><th>Severity</th><th>Message</th></tr>");
        foreach (var entry in request.AuditEntries)
        {
            builder.Append("<tr>");
            AddCell(builder, entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss zzz"));
            AddCell(builder, entry.Action);
            AddCell(builder, entry.Severity);
            AddCell(builder, entry.Message);
            builder.AppendLine("</tr>");
        }
        builder.AppendLine("</table>");

        if (!request.PatchPreview.IsAllowed || !request.SafetyValidation.IsAllowed || !request.ChecksumSupport.CanExportCalibrationOutput)
        {
            builder.AppendLine("<div class=\"blocked\">One or more gates are blocked. No real calibration output is allowed.</div>");
        }

        builder.AppendLine("</body></html>");
        return builder.ToString();
    }

    private static void AddSection(StringBuilder builder, string title, params (string Label, string? Value)[] rows)
    {
        builder.Append("<h2>");
        builder.Append(Html(title));
        builder.AppendLine("</h2><table>");
        foreach (var row in rows)
        {
            builder.Append("<tr><th>");
            builder.Append(Html(row.Label));
            builder.Append("</th><td>");
            builder.Append(Html(string.IsNullOrWhiteSpace(row.Value) ? "Unknown" : row.Value));
            builder.AppendLine("</td></tr>");
        }
        builder.AppendLine("</table>");
    }

    private static void AddCell(StringBuilder builder, string value)
    {
        builder.Append("<td>");
        builder.Append(Html(value));
        builder.Append("</td>");
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
