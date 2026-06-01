using SafeEcu.Application.Auditing;

namespace SafeEcu.Application.CalibrationPatching;

public sealed class CalibrationPatchPreviewService
{
    private readonly CalibrationMapLocator _mapLocator;
    private readonly CalibrationPatchValidationService _validationService;
    private readonly AuditLogService _auditLogService;

    public CalibrationPatchPreviewService(
        CalibrationMapLocator mapLocator,
        CalibrationPatchValidationService validationService,
        AuditLogService auditLogService)
    {
        _mapLocator = mapLocator;
        _validationService = validationService;
        _auditLogService = auditLogService;
    }

    public async Task<CalibrationPatchResult> PreviewAsync(
        CalibrationPatchPreviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var blockReasons = _validationService.Validate(request).ToList();
        var values = new List<CalibrationMapValue>();
        var definitionsById = request.MapDefinitions.ToDictionary(
            definition => definition.MapId,
            StringComparer.OrdinalIgnoreCase);

        foreach (var change in request.Changes)
        {
            if (!definitionsById.TryGetValue(change.MapId, out var definition))
            {
                continue;
            }

            var value = await _mapLocator.LocateAsync(
                request.OriginalFilePath,
                definition,
                change,
                cancellationToken);

            if (value is null)
            {
                blockReasons.Add($"Map '{change.MapId}' could not be located inside the expected address range.");
                continue;
            }

            values.Add(value);
        }

        var isAllowed = blockReasons.Count == 0;
        var result = new CalibrationPatchResult(
            isAllowed,
            isAllowed ? CalibrationPatchStatus.AllowedPreview : CalibrationPatchStatus.Blocked,
            new CalibrationChangeSet(request.ProfileKey, request.Mode, values, CreatesModifiedFile: false),
            isAllowed
                ? ["Patch preview completed. No modified ECU file was created."]
                : [],
            blockReasons);

        await _auditLogService.RecordAsync(new AuditLogRequest(
            "CalibrationPatch.Preview",
            "CalibrationPatch",
            request.ProfileKey,
            isAllowed ? "Information" : "Warning",
            isAllowed ? "Calibration patch preview allowed." : "Calibration patch preview blocked.",
            string.Join(Environment.NewLine, blockReasons),
            null,
            null), cancellationToken);

        return result;
    }
}
