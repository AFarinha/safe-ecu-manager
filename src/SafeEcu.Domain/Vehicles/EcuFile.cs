namespace SafeEcu.Domain.Vehicles;

public sealed class EcuFile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid VehicleId { get; set; }

    public Vehicle? Vehicle { get; set; }

    public Guid? EcuInfoId { get; set; }

    public EcuInfo? EcuInfo { get; set; }

    public EcuFileType FileType { get; set; } = EcuFileType.Unknown;

    public string FileName { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public string Sha256Hash { get; set; } = string.Empty;

    public DateTimeOffset ImportedAt { get; set; } = DateTimeOffset.UtcNow;

    public string? FileOrigin { get; set; }

    public string? ReadMethod { get; set; }

    public string? ProgrammerUsed { get; set; }

    public ChecksumStatus ChecksumStatus { get; set; } = ChecksumStatus.Unknown;

    public string? Notes { get; set; }
}
