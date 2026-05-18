namespace SafeEcu.Domain.Application;

public enum SupportStatus
{
    Unknown,
    NotSupported,
    ManualWorkflowOnly,
    FileManagement,
    ChecksumValidation,
    CalibrationPreview,
    Verified
}
