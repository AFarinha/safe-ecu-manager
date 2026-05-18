using SafeEcu.Application.Vehicles;
using SafeEcu.Domain.Vehicles;
using SafeEcu.Infrastructure.Persistence;
using SafeEcu.Infrastructure.Persistence.Repositories;
using SafeEcu.Tests.TestDoubles;

namespace SafeEcu.Tests.Vehicles;

public sealed class VehicleServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly SafeEcuDbContextFactory _dbContextFactory;
    private readonly NullAppLogger _logger = new();

    public VehicleServiceTests()
    {
        _testDirectory = Path.Combine(AppContext.BaseDirectory, "test-data", Guid.NewGuid().ToString("N"));
        var connectionString = SqliteConnectionStringFactory.Create(_testDirectory, "vehicle-service.db");
        _dbContextFactory = new SafeEcuDbContextFactory(connectionString);
    }

    [Fact]
    public async Task Create_rejects_invalid_year()
    {
        await InitializeDatabaseAsync();
        var service = CreateService();

        var result = await service.CreateAsync(new Vehicle
        {
            Make = "Opel",
            Model = "Corsa C",
            Year = 1800
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("Vehicle year is outside the supported range.", result.ErrorMessage);
    }

    [Fact]
    public async Task Update_persists_vehicle_changes()
    {
        await InitializeDatabaseAsync();
        var service = CreateService();

        var createResult = await service.CreateAsync(new Vehicle
        {
            Make = "Opel",
            Model = "Corsa C",
            Engine = "1.7 DTI",
            EngineCode = "Y17DT",
            FuelType = FuelType.Diesel
        });

        Assert.True(createResult.IsSuccess);
        var vehicle = createResult.Value!;
        vehicle.LicensePlate = "AA-00-AA";
        vehicle.EngineCode = "Y17DTI";

        var updateResult = await service.UpdateAsync(vehicle);
        var updatedVehicle = await service.GetByIdAsync(vehicle.Id);

        Assert.True(updateResult.IsSuccess);
        Assert.NotNull(updatedVehicle);
        Assert.Equal("AA-00-AA", updatedVehicle.LicensePlate);
        Assert.Equal("Y17DTI", updatedVehicle.EngineCode);
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    private VehicleService CreateService() =>
        new(new VehicleRepository(_dbContextFactory), _logger);

    private async Task InitializeDatabaseAsync()
    {
        var initializer = new SqlitePersistenceInitializer(_dbContextFactory, _logger);
        var result = await initializer.InitializeAsync();
        Assert.True(result.IsSuccess);
    }
}
