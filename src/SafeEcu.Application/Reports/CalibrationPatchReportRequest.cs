using SafeEcu.Application.CalibrationPatching;
using SafeEcu.Application.Checksums;
using SafeEcu.Application.EcuProfiles;
using SafeEcu.Application.Safety;
using SafeEcu.Domain.Auditing;

namespace SafeEcu.Application.Reports;

public sealed record CalibrationPatchReportRequest(
    string ReportsDirectory,
    string VehicleSummary,
    string EcuSummary,
    string OriginalFilePath,
    string OriginalSha256Hash,
    VerifiedEcuSoftwareProfile Profile,
    CalibrationPatchResult PatchPreview,
    CalibrationValidationResult SafetyValidation,
    EcuSoftwareChecksumSupportResult ChecksumSupport,
    IReadOnlyList<AuditLogEntry> AuditEntries);
