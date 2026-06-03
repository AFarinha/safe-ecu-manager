using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SafeEcu.Application.Common;
using SafeEcu.Application.Reports;
using SafeEcu.Infrastructure.Persistence;

namespace SafeEcu.Infrastructure.Reports;

public sealed class HtmlTechnicalReportService : IReportService
{
    private readonly SafeEcuDbContextFactory _dbContextFactory;
    private readonly IAppLogger _logger;

    public HtmlTechnicalReportService(SafeEcuDbContextFactory dbContextFactory, IAppLogger logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task<OperationResult<string>> GenerateTechnicalReportAsync(
        TechnicalReportRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ComparisonId == Guid.Empty)
        {
            return OperationResult<string>.Failure("A comparison must be selected before generating a report.");
        }

        await using var dbContext = _dbContextFactory.Create();
        var comparison = await dbContext.CalibrationComparisons
            .Include(item => item.OriginalFile)
                .ThenInclude(file => file!.Vehicle)
            .Include(item => item.OriginalFile)
                .ThenInclude(file => file!.EcuInfo)
            .Include(item => item.ModifiedFile)
                .ThenInclude(file => file!.Vehicle)
            .Include(item => item.ModifiedFile)
                .ThenInclude(file => file!.EcuInfo)
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == request.ComparisonId, cancellationToken);

        if (comparison is null || comparison.OriginalFile is null || comparison.ModifiedFile is null)
        {
            return OperationResult<string>.Failure("Comparison data could not be found.");
        }

        Directory.CreateDirectory(request.ReportsDirectory);
        var reportPath = Path.Combine(
            request.ReportsDirectory,
            $"technical-report-{comparison.Id:N}.html");
        var html = BuildHtml(comparison);

        await File.WriteAllTextAsync(reportPath, html, Encoding.UTF8, cancellationToken);
        _logger.Information($"Technical HTML report generated: {reportPath}.");

        return OperationResult<string>.Success(reportPath);
    }

    private static string BuildHtml(Domain.Calibrations.CalibrationComparison comparison)
    {
        var original = comparison.OriginalFile!;
        var modified = comparison.ModifiedFile!;
        var vehicle = original.Vehicle;
        var ecu = original.EcuInfo;

        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"en\">");
        builder.AppendLine("<head>");
        builder.AppendLine("<meta charset=\"utf-8\">");
        builder.AppendLine("<title>Safe ECU Technical Report</title>");
        builder.AppendLine("""
            <style>
            body { font-family: Segoe UI, Arial, sans-serif; margin: 32px; color: #17202A; }
            h1 { margin-bottom: 4px; }
            h2 { margin-top: 28px; border-bottom: 1px solid #D6DEE6; padding-bottom: 6px; }
            table { border-collapse: collapse; width: 100%; margin-top: 10px; }
            th, td { border: 1px solid #D6DEE6; padding: 8px; text-align: left; vertical-align: top; }
            th { background: #F4F6F8; width: 28%; }
            .warning { background: #FFF7E6; border: 1px solid #E2B96B; padding: 12px; margin-top: 18px; }
            .muted { color: #5F6C7B; }
            </style>
            """);
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");
        builder.AppendLine("<h1>Safe ECU Technical Report</h1>");
        builder.AppendLine($"<div class=\"muted\">Generated at {Html(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz"))}</div>");

        builder.AppendLine("<div class=\"warning\">Checksum valid does not mean calibration safe. It only means that the supported validation structure did not detect an error.</div>");

        builder.AppendLine("<h2>Vehicle</h2>");
        builder.AppendLine("<table>");
        AddRow(builder, "Make", vehicle?.Make);
        AddRow(builder, "Model", vehicle?.Model);
        AddRow(builder, "Year", vehicle?.Year?.ToString());
        AddRow(builder, "Engine", vehicle?.Engine);
        AddRow(builder, "Engine code", vehicle?.EngineCode);
        AddRow(builder, "VIN", vehicle?.Vin);
        AddRow(builder, "License plate", vehicle?.LicensePlate);
        builder.AppendLine("</table>");

        builder.AppendLine("<h2>ECU/EDU</h2>");
        builder.AppendLine("<table>");
        AddRow(builder, "Manufacturer", ecu?.Manufacturer);
        AddRow(builder, "Family", ecu?.EcuFamily);
        AddRow(builder, "Hardware reference", ecu?.HardwareReference);
        AddRow(builder, "Software reference", ecu?.SoftwareReference);
        AddRow(builder, "Software version", ecu?.SoftwareVersion);
        AddRow(builder, "Protocol", ecu?.Protocol);
        AddRow(builder, "Support status", ecu?.SupportStatus.ToString());
        builder.AppendLine("</table>");

        builder.AppendLine("<h2>Original file</h2>");
        AddFileTable(builder, original);

        builder.AppendLine("<h2>Modified file</h2>");
        AddFileTable(builder, modified);

        builder.AppendLine("<h2>Binary comparison</h2>");
        builder.AppendLine("<table>");
        AddRow(builder, "Result", comparison.Result);
        AddRow(builder, "Differences", comparison.DifferenceCount.ToString());
        AddRow(builder, "Percent changed", $"{comparison.PercentChanged:0.######}%");
        AddRow(builder, "Changed offsets sample", comparison.DifferenceSummary);
        AddRow(builder, "Changed blocks sample", comparison.DifferenceBlockSummary);
        AddRow(builder, "Compared at", comparison.ComparedAt.ToString("yyyy-MM-dd HH:mm:ss zzz"));
        builder.AppendLine("</table>");

        builder.AppendLine("<h2>Safety notes</h2>");
        builder.AppendLine("<ul>");
        builder.AppendLine("<li>This report does not approve ECU writing or flashing.</li>");
        builder.AppendLine("<li>This report does not interpret calibration maps.</li>");
        builder.AppendLine("<li>Unknown or NotSupported data must block advanced operations.</li>");
        builder.AppendLine("</ul>");

        builder.AppendLine("</body>");
        builder.AppendLine("</html>");
        return builder.ToString();
    }

    private static void AddFileTable(StringBuilder builder, Domain.Vehicles.EcuFile file)
    {
        builder.AppendLine("<table>");
        AddRow(builder, "File name", file.FileName);
        AddRow(builder, "File path", file.FilePath);
        AddRow(builder, "File type", file.FileType.ToString());
        AddRow(builder, "Size bytes", file.SizeBytes.ToString());
        AddRow(builder, "SHA-256", file.Sha256Hash);
        AddRow(builder, "Checksum status", file.ChecksumStatus.ToString());
        AddRow(builder, "File origin", file.FileOrigin);
        AddRow(builder, "Read method", file.ReadMethod);
        AddRow(builder, "Programmer used", file.ProgrammerUsed);
        builder.AppendLine("</table>");
    }

    private static void AddRow(StringBuilder builder, string label, string? value)
    {
        builder.Append("<tr><th>");
        builder.Append(Html(label));
        builder.Append("</th><td>");
        builder.Append(Html(string.IsNullOrWhiteSpace(value) ? "Unknown" : value));
        builder.AppendLine("</td></tr>");
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
