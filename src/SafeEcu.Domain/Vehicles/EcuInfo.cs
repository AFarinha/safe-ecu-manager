using SafeEcu.Domain.Application;

namespace SafeEcu.Domain.Vehicles;

public sealed class EcuInfo
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid VehicleId { get; set; }

    public Vehicle? Vehicle { get; set; }

    public string? Manufacturer { get; set; }

    public string? EcuFamily { get; set; }

    public string? HardwareReference { get; set; }

    public string? SoftwareReference { get; set; }

    public string? SoftwareVersion { get; set; }

    public string? Protocol { get; set; }

    public SupportStatus SupportStatus { get; set; } = SupportStatus.Unknown;

    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<EcuFile> EcuFiles { get; set; } = [];
}
