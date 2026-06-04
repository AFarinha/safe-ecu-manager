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
    public async Task SeedIfEmpty_creates_renault_megane_3_and_opel_corsa_c_with_candidate_ecus()
    {
        await InitializeDatabaseAsync();
        var vehicleRepository = new VehicleRepository(_dbContextFactory);
        var ecuRepository = new EcuInfoRepository(_dbContextFactory);
        var seeder = new RenaultMegane3BaselineSeeder(vehicleRepository, ecuRepository, _logger);

        await seeder.SeedIfEmptyAsync();

        var vehicles = await vehicleRepository.ListAsync();
        Assert.Equal(2, vehicles.Count);
        var renault = Assert.Single(vehicles, vehicle => vehicle.Make == "Renault");
        Assert.Equal("Megane 3", renault.Model);
        Assert.Equal("K9K", renault.EngineCode);
        var opel = Assert.Single(vehicles, vehicle => vehicle.Make == "Opel");
        Assert.Equal("Corsa C", opel.Model);
        Assert.Equal("Y17DT/Y17DTI", opel.EngineCode);

        var renaultEcus = await ecuRepository.ListByVehicleAsync(renault.Id);
        Assert.Equal(2, renaultEcus.Count);
        Assert.Contains(renaultEcus, ecu => ecu.Manufacturer == "Delphi" && ecu.EcuFamily == "Delphi DCM");
        Assert.Contains(renaultEcus, ecu => ecu.Manufacturer == "Bosch" && ecu.EcuFamily == "Bosch EDC");
        Assert.All(renaultEcus, ecu => Assert.Equal(SupportStatus.FileManagement, ecu.SupportStatus));

        var opelEcus = await ecuRepository.ListByVehicleAsync(opel.Id);
        Assert.Equal(2, opelEcus.Count);
        Assert.Contains(opelEcus, ecu => ecu.Manufacturer == "Delco/Delphi/Isuzu" && ecu.EcuFamily == "Delco/Delphi/Isuzu Opel 1.7 DTI EDU");
        Assert.Contains(opelEcus, ecu => ecu.Manufacturer == "Bosch" && ecu.EcuFamily == "Bosch VP44 PSG5/PSG16");
        Assert.All(opelEcus, ecu => Assert.Equal(SupportStatus.FileManagement, ecu.SupportStatus));
    }

    [Fact]
    public async Task SeedIfEmpty_adds_missing_candidates_without_duplicating_existing_records()
    {
        await InitializeDatabaseAsync();
        var vehicleRepository = new VehicleRepository(_dbContextFactory);
        var ecuRepository = new EcuInfoRepository(_dbContextFactory);
        await vehicleRepository.AddAsync(new Vehicle
        {
            Make = "Renault",
            Model = "Megane 3",
            Engine = "1.5 dCi",
            EngineCode = "K9K"
        });
        var seeder = new RenaultMegane3BaselineSeeder(vehicleRepository, ecuRepository, _logger);

        await seeder.SeedIfEmptyAsync();
        await seeder.SeedIfEmptyAsync();

        var vehicles = await vehicleRepository.ListAsync();
        Assert.Equal(2, vehicles.Count);
        var renault = Assert.Single(vehicles, vehicle => vehicle.Make == "Renault");
        var opel = Assert.Single(vehicles, vehicle => vehicle.Make == "Opel");
        var renaultEcus = await ecuRepository.ListByVehicleAsync(renault.Id);
        var opelEcus = await ecuRepository.ListByVehicleAsync(opel.Id);
        Assert.Equal(2, renaultEcus.Count);
        Assert.Equal(2, opelEcus.Count);
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
