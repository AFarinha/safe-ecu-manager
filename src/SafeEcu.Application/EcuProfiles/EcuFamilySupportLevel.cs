namespace SafeEcu.Application.EcuProfiles;

public enum EcuFamilySupportLevel
{
    Unknown,
    NotSupported,
    ManualWorkflowOnly,
    FileManagement,
    ChecksumValidation,
    MapPreview,
    CalibrationProfilePreview,
    WriteNotSupported,
    Verified
}
