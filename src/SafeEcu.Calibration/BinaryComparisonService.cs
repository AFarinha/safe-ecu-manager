using SafeEcu.Application.Calibrations;
using SafeEcu.Application.Common;
using SafeEcu.Application.Persistence;
using SafeEcu.Domain.Calibrations;
using SafeEcu.Domain.Vehicles;

namespace SafeEcu.Calibration;

public sealed class BinaryComparisonService
{
    private readonly IEcuFileRepository _ecuFileRepository;
    private readonly ICalibrationComparisonRepository _comparisonRepository;
    private readonly IAppLogger _logger;

    public BinaryComparisonService(
        IEcuFileRepository ecuFileRepository,
        ICalibrationComparisonRepository comparisonRepository,
        IAppLogger logger)
    {
        _ecuFileRepository = ecuFileRepository;
        _comparisonRepository = comparisonRepository;
        _logger = logger;
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

        var differenceCount = await CountDifferencesAsync(originalFile.FilePath, modifiedFile.FilePath, cancellationToken);
        var comparedLength = Math.Max(originalFile.SizeBytes, modifiedFile.SizeBytes);
        var percentChanged = comparedLength == 0
            ? 0
            : decimal.Round(differenceCount * 100m / comparedLength, 6);

        var comparison = new CalibrationComparison
        {
            OriginalFileId = originalFile.Id,
            ModifiedFileId = modifiedFile.Id,
            DifferenceCount = differenceCount,
            PercentChanged = percentChanged,
            Result = differenceCount == 0 ? "Identical" : "Different",
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

    private static async Task<long> CountDifferencesAsync(
        string originalFilePath,
        string modifiedFilePath,
        CancellationToken cancellationToken)
    {
        const int bufferSize = 81920;
        var differences = 0L;

        await using var originalStream = File.OpenRead(originalFilePath);
        await using var modifiedStream = File.OpenRead(modifiedFilePath);

        var originalBuffer = new byte[bufferSize];
        var modifiedBuffer = new byte[bufferSize];

        while (true)
        {
            var originalRead = await originalStream.ReadAsync(originalBuffer, cancellationToken);
            var modifiedRead = await modifiedStream.ReadAsync(modifiedBuffer, cancellationToken);
            var maxRead = Math.Max(originalRead, modifiedRead);

            if (maxRead == 0)
            {
                break;
            }

            var minRead = Math.Min(originalRead, modifiedRead);
            for (var index = 0; index < minRead; index++)
            {
                if (originalBuffer[index] != modifiedBuffer[index])
                {
                    differences++;
                }
            }

            differences += Math.Abs(originalRead - modifiedRead);
        }

        return differences;
    }
}
