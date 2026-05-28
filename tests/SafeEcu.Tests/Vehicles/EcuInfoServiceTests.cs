using SafeEcu.Application.Vehicles;
using SafeEcu.Domain.Application;
using SafeEcu.Domain.Vehicles;
using SafeEcu.Infrastructure.Persistence;
using SafeEcu.Infrastructure.Persistence.Repositories;
using SafeEcu.Tests.TestDoubles;

namespace SafeEcu.Tests.Vehicles;

public sealed class EcuInfoServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly SafeEcuDbContextFactory _dbContextFactory;
    private readonly NullAppLogger _logger = new();

    public EcuInfoServiceTests()
    {
        _testDirectory = Path.Combine(AppContext.BaseDirectory, "ecu-info-tests", Guid.NewGuid().ToString("N"));
        var connectionString = SqliteConnectionStringFactory.Create(_testDirectory, "ecu-info-tests.db");
        _dbContextFactory = new SafeEcuDbContextFactory(connectionString);
    }

    [Fact]
    public async Task Update_persists_manual_identification_fields()
    {
        await InitializeDatabaseAsync();
        var vehicle = new Vehicle { Make = "Opel", Model = "Corsa C" };
        await new VehicleRepository(_dbContextFactory).AddAsync(vehicle);

        var service = new EcuInfoService(new EcuInfoRepository(_dbContextFactory), _logger);
        var create = await service.CreateAsync(new EcuInfo
        {
            VehicleId = vehicle.Id,
            Manufacturer = "Unknown",
            EcuFamily = "Unknown"
        });

        var ecu = create.Value!;
        ecu.Manufacturer = "Delphi";
        ecu.EcuFamily = "DCM";
        ecu.SoftwareVersion = "Manual";
        ecu.SupportStatus = SupportStatus.FileManagement;

        var update = await service.UpdateAsync(ecu);
        var saved = await service.GetByIdAsync(ecu.Id);

        Assert.True(update.IsSuccess);
        Assert.NotNull(saved);
        Assert.Equal("Delphi", saved.Manufacturer);
        Assert.Equal(SupportStatus.FileManagement, saved.SupportStatus);
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
