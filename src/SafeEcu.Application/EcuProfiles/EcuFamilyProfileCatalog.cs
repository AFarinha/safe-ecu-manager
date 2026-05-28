namespace SafeEcu.Application.EcuProfiles;

public sealed class EcuFamilyProfileCatalog
{
    public IReadOnlyList<EcuFamilyProfile> Profiles { get; } =
    [
        new(
            "Bosch EDC15",
            "Bosch",
            EcuFamilySupportLevel.FileManagement,
            [".bin", ".ori", ".mod"],
            [],
            "Metadata profile only. No map interpretation, checksum algorithm or write support is implemented."),
        new(
            "Bosch EDC16",
            "Bosch",
            EcuFamilySupportLevel.FileManagement,
            [".bin", ".ori", ".mod"],
            [],
            "Metadata profile only. Exact ECU/software version must be confirmed manually."),
        new(
            "Bosch EDC17",
            "Bosch",
            EcuFamilySupportLevel.NotSupported,
            [".bin", ".ori", ".mod"],
            [],
            "Locked/protected ECUs are not supported. No tuning protection bypass is implemented."),
        new(
            "Delphi DCM",
            "Delphi",
            EcuFamilySupportLevel.FileManagement,
            [".bin", ".ori", ".mod"],
            [],
            "Metadata profile only. Used for cautious Renault K9K identification until exact ECU is confirmed."),
        new(
            "Siemens/Continental SID",
            "Siemens/Continental",
            EcuFamilySupportLevel.Unknown,
            [".bin", ".ori", ".mod"],
            [],
            "Unknown support. Requires concrete test files and validated documentation."),
        new(
            "Delco/Delphi/Isuzu Opel 1.7 DTI",
            "Delco/Delphi/Isuzu",
            EcuFamilySupportLevel.FileManagement,
            [".bin", ".ori", ".mod"],
            [],
            "Priority metadata profile for Opel Corsa C 1.7 DTI. ECU/EDU reference, software version, size and original hash must be confirmed."),
        new(
            "Bosch VP44 PSG5/PSG16",
            "Bosch",
            EcuFamilySupportLevel.Unknown,
            [".bin", ".ori", ".mod"],
            [],
            "Unknown support. No direct hardware operation, recovery, boot or bench mode support.")
    ];
}
