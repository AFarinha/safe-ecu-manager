using SafeEcu.Application.Checksums;
using SafeEcu.Application.EcuProfiles;
using SafeEcu.Application.Safety;

namespace SafeEcu.Application.MapWorkspace;

public sealed record MapChangeExportGateRequest(
    RenaultMegane3EvidenceResult EvidenceResult,
    PercentageMapChangePlanResult ChangePlan,
    EcuSoftwareChecksumSupportResult ChecksumSupport,
    CalibrationValidationResult SafetyValidation);
