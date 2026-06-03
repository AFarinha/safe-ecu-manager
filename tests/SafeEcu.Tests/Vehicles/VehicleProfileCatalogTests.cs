using SafeEcu.Application.Vehicles;
using SafeEcu.Domain.Vehicles;

namespace SafeEcu.Tests.Vehicles;

public sealed class VehicleProfileCatalogTests
{
    [Fact]
    public void Includes_initial_safe_vehicle_profiles()
    {
        var catalog = new VehicleProfileCatalog();

        Assert.Equal("renault-megane-3-k9k", catalog.Profiles[0].Id);

        Assert.Contains(catalog.Profiles, profile =>
            profile.Make == "Opel" &&
            profile.Model == "Corsa C" &&
            profile.EngineCode == "Y17DT/Y17DTI" &&
            profile.FuelType == FuelType.Diesel);

        Assert.Contains(catalog.Profiles, profile =>
            profile.Make == "Renault" &&
            profile.Model == "Megane 3" &&
            profile.EngineCode == "K9K" &&
            profile.FuelType == FuelType.Diesel);
    }
}
