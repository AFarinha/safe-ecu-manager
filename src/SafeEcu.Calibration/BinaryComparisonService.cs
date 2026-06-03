using SafeEcu.Application.Calibrations;
using SafeEcu.Application.Common;
using SafeEcu.Application.Persistence;
using SafeEcu.Domain.Calibrations;
using SafeEcu.Domain.Vehicles;
using System.Globalization;
using System.Text;

namespace SafeEcu.Calibration;

public sealed class BinaryComparisonService
{
    private readonly IEcuFileRepository _ecuFileRepository;
    private readonly ICalibrationComparisonRepository _comparisonRepository;
    private readonly IAppLogger _logger;
    private readonly AdvancedBinaryDifferenceService _differenceService;

    public BinaryComparisonService(
        IEcuFileRepository ecuFileRepository,
        ICalibrationComparisonRepository comparisonRepository,
        IAppLogger logger,
        AdvancedBinaryDifferenceService? differenceService = null)
    {
        _ecuFileRepository = ecuFileRepository;
        _comparisonRepository = comparisonRepository;
        _logger = logger;
        _differenceService = differenceService ?? new AdvancedBinaryDifferenceService();
    }

    public async Task<OperationResult<CalibrationComparison>> CompareAsync(
        BinaryComparisonRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.OriginalFileId == request.ModifiedFileId)
        {
            return OperationResult<CalibrationComparison>.Failure("Original and modified ECU files must be different records.");
        }

        var original = await _ecuFileRepository.GetByIdAsync(request.OriginalFileId, cancellationToken);
        var modified = await _ecuFileRepository.GetByIdAsync(request.ModifiedFileId, cancellationToken);

        var validation = ValidateFiles(original, modified);
        if (!validation.IsSuccess)
        {
            return OperationResult<CalibrationComparison>.Failure(validation.ErrorMessage!);
        }

        var originalFile = original!;
        var modifiedFile = modified!;

        var scanResult = await _differenceService.ScanAsync(
            originalFile.FilePath,
            modifiedFile.FilePath,
            BinaryDifferenceScanOptions.Default,
            cancellationToken);

        var comparison = new CalibrationComparison
        {
            OriginalFileId = originalFile.Id,
            ModifiedFileId = modifiedFile.Id,
            DifferenceCount = scanResult.DifferenceCount,
            PercentChanged = scanResult.PercentChanged,
            Result = scanResult.DifferenceCount == 0 ? "Identical" : "Different",
            DifferenceSummary = BuildDifferenceSummary(scanResult),
            DifferenceBlockSummary = BuildBlockSummary(scanResult),
            ComparedAt = DateTimeOffset.UtcNow
        };

        await _comparisonRepository.AddAsync(comparison, cancellationToken);
        _logger.Information($"Binary comparison created: {comparison.Id}.");

        return OperationResult<CalibrationComparison>.Success(comparison);
    }

    public Task<IReadOnlyList<CalibrationComparison>> ListAsync(CancellationToken cancellationToken = default) =>
        _comparisonRepository.ListAsync(cancellationToken);

    private static OperationResult ValidateFiles(EcuFile? original, EcuFile? modified)
    {
        if (original is null)
        {
            return OperationResult.Failure("Original ECU file was not found.");
        }

        if (modified is null)
        {
            return OperationResult.Failure("Modified ECU file was not found.");
        }

        if (original.FileType != EcuFileType.Original)
        {
            return OperationResult.Failure("The selected original file must be marked as Original.");
        }

        if (modified.FileType != EcuFileType.Modified)
        {
            return OperationResult.Failure("The selected modified file must be marked as Modified.");
        }

        if (original.VehicleId != modified.VehicleId)
        {
            return OperationResult.Failure("Files must belong to the same vehicle.");
        }

        if (original.EcuInfoId is null || modified.EcuInfoId is null || original.EcuInfoId != modified.EcuInfoId)
        {
            return OperationResult.Failure("Files must belong to the same confirmed ECU/EDU record.");
        }

        if (string.Equals(original.Sha256Hash, modified.Sha256Hash, StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult.Failure("Files have the same SHA-256 hash and do not need binary comparison.");
        }

        if (!File.Exists(original.FilePath))
        {
            return OperationResult.Failure("Original ECU file backup could not be found.");
        }

        if (!File.Exists(modified.FilePath))
        {
            return OperationResult.Failure("Modified ECU file backup could not be found.");
        }

        return OperationResult.Success();
    }

    private static string BuildDifferenceSummary(BinaryDifferenceScanResult scanResult)
    {
        if (scanResult.SampledDifferences.Count == 0)
        {
            return "No byte offsets changed.";
        }

        var builder = new StringBuilder();
        foreach (var difference in scanResult.SampledDifferences.Take(50))
        {
            if (builder.Length > 0)
            {
                builder.Append("; ");
            }

            builder
                .Append("0x")
                .Append(difference.Offset.ToString("X8", CultureInfo.InvariantCulture))
                .Append(": ")
                .Append(FormatByte(difference.OriginalByte))
                .Append(" -> ")
                .Append(FormatByte(difference.ModifiedByte));
        }

        if (scanResult.IsSampleTruncated || scanResult.SampledDifferences.Count > 50)
        {
            builder.Append("; sample truncated");
        }

        return builder.ToString();
    }

    private static string BuildBlockSummary(BinaryDifferenceScanResult scanResult)
    {
        if (scanResult.Blocks.Count == 0)
        {
            return "No changed byte blocks.";
        }

        var builder = new StringBuilder();
        foreach (var block in scanResult.Blocks.Take(25))
        {
            if (builder.Length > 0)
            {
                builder.Append("; ");
            }

            builder
                .Append("0x")
                .Append(block.StartOffset.ToString("X8", CultureInfo.InvariantCulture))
                .Append("-0x")
                .Append(block.EndOffset.ToString("X8", CultureInfo.InvariantCulture))
                .Append(" (")
                .Append(block.DifferenceCount.ToString(CultureInfo.InvariantCulture))
                .Append(" differences)");
        }

        if (scanResult.AreBlocksTruncated || scanResult.Blocks.Count > 25)
        {
            builder.Append("; block list truncated");
        }

        return builder.ToString();
    }

    private static string FormatByte(byte? value) =>
        value is null ? "Missing" : $"0x{value.Value:X2}";
}
