using SafeEcu.Domain.Vehicles;

namespace SafeEcu.Domain.Projects;

public sealed class EcuProjectFileVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProjectId { get; set; }

    public EcuProject? Project { get; set; }

    public Guid EcuFileId { get; set; }

    public EcuFile? EcuFile { get; set; }

    public EcuProjectFileVersionKind Kind { get; set; } = EcuProjectFileVersionKind.Modified;

    public int VersionNumber { get; set; }

    public string Label { get; set; } = string.Empty;

    public bool IsOriginalProtected { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
