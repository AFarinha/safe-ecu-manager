using SafeEcu.Domain.Vehicles;

namespace SafeEcu.Domain.Projects;

public sealed class EcuProject
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid VehicleId { get; set; }

    public Vehicle? Vehicle { get; set; }

    public Guid? EcuInfoId { get; set; }

    public EcuInfo? EcuInfo { get; set; }

    public Guid OriginalFileId { get; set; }

    public EcuFile? OriginalFile { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Make { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string Engine { get; set; } = string.Empty;

    public int? Year { get; set; }

    public string EcuType { get; set; } = string.Empty;

    public string? EcuReference { get; set; }

    public string ReadType { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public string? Tags { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<EcuProjectFileVersion> Versions { get; set; } = [];
}
