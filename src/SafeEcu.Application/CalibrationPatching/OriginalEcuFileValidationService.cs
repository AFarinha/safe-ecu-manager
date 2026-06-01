using SafeEcu.Application.Files;

namespace SafeEcu.Application.CalibrationPatching;

public sealed class OriginalEcuFileValidationService
{
    private readonly IFileHashService _fileHashService;
    private readonly CalibrationMapDefinitionValidationService _mapDefinitionValidationService;

    public OriginalEcuFileValidationService(
        IFileHashService fileHashService,
        CalibrationMapDefinitionValidationService mapDefinitionValidationService)
    {
        _fileHashService = fileHashService;
        _mapDefinitionValidationService = mapDefinitionValidationService;
    }

    public async Task<OriginalEcuFileValidationResult> ValidateAsync(
        OriginalEcuFileValidationRequest request,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<string>();
        var blockReasons = new List<string>();

        if (!File.Exists(request.OriginalFilePath))
        {
            blockReasons.Add("Original ECU file does not exist.");
        }

        if (!File.Exists(request.BackupFilePath))
        {
            blockReasons.Add("Verified backup file does not exist.");
        }

        if (blockReasons.Count == 0)
        {
            var fileInfo = new FileInfo(request.OriginalFilePath);
            if (fileInfo.Length != request.Profile.ExpectedFileSizeBytes)
            {
                blockReasons.Add(
                    $"Original file size {fileInfo.Length} does not match expected profile size {request.Profile.ExpectedFileSizeBytes}.");
            }

            var hash = await _fileHashService.ComputeSha256Async(request.OriginalFilePath, cancellationToken);
            if (!string.Equals(hash, request.Profile.OriginalSha256Hash, StringComparison.OrdinalIgnoreCase))
            {
                blockReasons.Add("Original SHA-256 hash does not match the verified ECU software profile.");
            }

            foreach (var map in request.MapDefinitions)
            {
                if (map.AddressRange.EndOffset >= fileInfo.Length)
                {
                    blockReasons.Add($"Map '{map.MapId}' address range exceeds original file size.");
                }
            }
        }

        var mapValidation = _mapDefinitionValidationService.Validate(
            new CalibrationMapDefinitionValidationRequest(request.Profile, request.MapDefinitions));
        blockReasons.AddRange(mapValidation.BlockReasons);

        if (blockReasons.Count == 0)
        {
            messages.Add("Original ECU file, backup, hash, size and map ranges match the verified dummy/test profile.");
        }

        return new OriginalEcuFileValidationResult(blockReasons.Count == 0, messages, blockReasons);
    }
}
