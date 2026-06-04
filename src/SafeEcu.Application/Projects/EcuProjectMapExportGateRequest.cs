namespace SafeEcu.Application.Projects;

public sealed record EcuProjectMapExportGateRequest(
    Guid ProjectId,
    EcuProjectMapEditPreviewResult EditPreview,
    bool ChecksumSupported,
    bool ChecksumWillBeRecalculated,
    bool ExplicitUserConfirmation,
    string SupportStatus);
