using SafeEcu.Application.Vehicles;
using SafeEcu.Domain.Application;
using SafeEcu.Domain.Vehicles;
using SafeEcu.Infrastructure.Persistence;
using SafeEcu.Infrastructure.Persistence.Repositories;
using SafeEcu.Tests.TestDoubles;

namespace SafeEcu.Tests.Vehicles;

public sealed class RenaultMegane3BaselineSeederTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly SafeEcuDbContextFactory _dbContextFactory;
    private readonly NullAppLogger _logger = new();

    public RenaultMegane3BaselineSeederTests()
    {
        _testDirectory = Path.Combine(AppContext.BaseDirectory, "renault-megane-seed-tests", Guid.NewGuid().ToString("N"));
        var connectionString = SqliteConnectionStringFactory.Create(_testDirectory, "renault-seed.db");
        _dbContextFactory = new SafeEcuDbContextFactory(connectionString);
    }

    [Fact]
    public async Task SeedIfEmpty_creates_renault_megane_3_with_candidate_ecus()
    {
        await InitializeDatabaseAsync();
        var vehicleRepository = new VehicleRepository(_dbContextFactory);
        var ecuRepository = new EcuInfoRepository(_dbContextFactory);
        var seeder = new RenaultMegane3BaselineSeeder(vehicleRepository, ecuRepository, _logger);

        await seeder.SeedIfEmptyAsync();

        var vehicles = await vehicleRepository.ListAsync();
        var vehicle = Assert.Single(vehicles);
        Assert.Equal("Renault", vehicle.Make);
        Assert.Equal("Megane 3", vehicle.Model);
        Assert.Equal("K9K", vehicle.EngineCode);

        var ecus = await ecuRepository.ListByVehicleAsync(vehicle.Id);
        Assert.Equal(2, ecus.Count);
        Assert.Contains(ecus, ecu => ecu.Manufacturer == "Delphi" && ecu.EcuFamily == "Delphi DCM");
        Assert.Contains(ecus, ecu => ecu.Manufacturer == "Bosch" && ecu.EcuFamily == "Bosch EDC");
        Assert.All(ecus, ecu => Assert.Equal(SupportStatus.FileManagement, ecu.SupportStatus));
    }

    [Fact]
    public async Task SeedIfEmpty_does_not_duplicate_when_records_exist()
    {
        await InitializeDatabaseAsync();
        var vehicleRepository = new VehicleRepository(_dbContextFactory);
        var ecuRepository = new EcuInfoRepository(_dbContextFactory);
        await vehicleRepository.AddAsync(new Vehicle { Make = "Renault", Model = "Megane 3" });
        var seeder = new RenaultMegane3BaselineSeeder(vehicleRepository, ecuRepository, _logger);

        await seeder.SeedIfEmptyAsync();

        var vehicles = await vehicleRepository.ListAsync();
        Assert.Single(vehicles);
        var ecus = await ecuRepository.ListByVehicleAsync(vehicles[0].Id);
        Assert.Empty(ecus);
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    private async Task InitializeDatabaseAsync()
    {
        var initializer = new SqlitePersistenceInitializer(_dbContextFactory, _logger);
        var result = await initializer.InitializeAsync();
        Assert.True(result.IsSuccess);
    }
}
