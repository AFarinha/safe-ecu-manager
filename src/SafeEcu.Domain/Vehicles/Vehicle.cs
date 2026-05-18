namespace SafeEcu.Domain.Vehicles;

public sealed class Vehicle
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string? Vin { get; set; }

    public string? LicensePlate { get; set; }

    public string Make { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public int? Year { get; set; }

    public string Engine { get; set; } = string.Empty;

    public string EngineCode { get; set; } = string.Empty;

    public FuelType FuelType { get; set; } = FuelType.Unknown;

    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<EcuInfo> Ecus { get; set; } = [];

    public List<EcuFile> EcuFiles { get; set; } = [];
}
