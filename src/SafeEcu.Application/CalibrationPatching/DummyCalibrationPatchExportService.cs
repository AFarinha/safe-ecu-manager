using SafeEcu.Application.Auditing;
using SafeEcu.Application.EcuProfiles;
using SafeEcu.Application.Files;

namespace SafeEcu.Application.CalibrationPatching;

public sealed class DummyCalibrationPatchExportService
{
    private readonly IFileHashService _fileHashService;
    private readonly AuditLogService _auditLogService;

    public DummyCalibrationPatchExportService(
        IFileHashService fileHashService,
        AuditLogService auditLogService)
    {
        _fileHashService = fileHashService;
        _auditLogService = auditLogService;
    }

    public async Task<DummyCalibrationPatchExportResult> ExportAsync(
        DummyCalibrationPatchExportRequest request,
        CancellationToken cancellationToken = default)
    {
        var blockReasons = Validate(request).ToList();
        if (blockReasons.Count > 0)
        {
            await AuditAsync(request.Profile.ProfileId, false, blockReasons, cancellationToken);
            return new DummyCalibrationPatchExportResult(false, false, null, null, [], blockReasons);
        }

        Directory.CreateDirectory(request.OutputDirectory);
        var outputPath = Path.Combine(
            request.OutputDirectory,
            $"{Path.GetFileNameWithoutExtension(request.OriginalFilePath)}.dummy-modified{Path.GetExtension(request.OriginalFilePath)}");

        var bytes = await File.ReadAllBytesAsync(request.OriginalFilePath, cancellationToken);
        foreach (var value in request.ValidatedChangeSet.ChangeSet!.Values)
        {
            if (value.Offset < 0 || value.Offset >= bytes.Length)
            {
                var reason = $"Patch offset {value.Offset} is outside the dummy file.";
                await AuditAsync(request.Profile.ProfileId, false, [reason], cancellationToken);
                return new DummyCalibrationPatchExportResult(false, false, null, null, [], [reason]);
            }

            bytes[value.Offset] = value.ProposedValue;
        }

        await File.WriteAllBytesAsync(outputPath, bytes, cancellationToken);
        var outputHash = await _fileHashService.ComputeSha256Async(outputPath, cancellationToken);

        await AuditAsync(request.Profile.ProfileId, true, [], cancellationToken);
        return new DummyCalibrationPatchExportResult(
            true,
            true,
            outputPath,
            outputHash,
            ["Dummy patch export completed. This is not a real ECU export workflow."],
            []);
    }

    private static IEnumerable<string> Validate(DummyCalibrationPatchExportRequest request)
    {
        if (!File.Exists(request.OriginalFilePath))
        {
            yield return "Original dummy file does not exist.";
        }

        if (!IsDummyOrTestProfile(request.Profile))
        {
            yield return "Dummy export is blocked for non-dummy ECU software profiles.";
        }

        if (request.Profile.SupportStatus != VerifiedEcuSoftwareSupportStatus.Verified)
        {
            yield return "Dummy export requires a verified dummy ECU software profile.";
        }

        if (!string.Equals(request.Profile.ChecksumAlgorithmId, "dummy-checksum", StringComparison.OrdinalIgnoreCase))
        {
            yield return "Dummy export requires the dummy checksum algorithm id.";
        }

        if (!request.ValidatedChangeSet.IsAllowed || request.ValidatedChangeSet.ChangeSet is null)
        {
            yield return "Validated change set is required before dummy export.";
        }
    }

    private Task AuditAsync(
        string profileId,
        bool allowed,
        IReadOnlyList<string> blockReasons,
        CancellationToken cancellationToken) =>
        _auditLogService.RecordAsync(new AuditLogRequest(
            "CalibrationPatch.DummyExport",
            "CalibrationPatch",
            profileId,
            allowed ? "Information" : "Warning",
            allowed ? "Dummy calibration patch export allowed." : "Dummy calibration patch export blocked.",
            string.Join(Environment.NewLine, blockReasons),
            null,
            null), cancellationToken);

    private static bool IsDummyOrTestProfile(VerifiedEcuSoftwareProfile profile) =>
        profile.ProfileId.Contains("dummy", StringComparison.OrdinalIgnoreCase)
        || profile.ProfileId.Contains("test", StringComparison.OrdinalIgnoreCase)
        || profile.Source.Contains("unit test", StringComparison.OrdinalIgnoreCase);
}
