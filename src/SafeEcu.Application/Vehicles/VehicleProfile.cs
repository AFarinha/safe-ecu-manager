using SafeEcu.Domain.Vehicles;

namespace SafeEcu.Application.Vehicles;

public sealed record VehicleProfile(
    string Id,
    string DisplayName,
    string Make,
    string Model,
    string Engine,
    string EngineCode,
    FuelType FuelType,
    string Notes);
