using SafeEcu.Application.Vehicles;
using SafeEcu.Domain.Application;
using SafeEcu.Domain.Vehicles;
using SafeEcu.Infrastructure.Persistence;
using SafeEcu.Infrastructure.Persistence.Repositories;
using SafeEcu.Tests.TestDoubles;
using Microsoft.Data.Sqlite;

namespace SafeEcu.Tests.Persistence;

public sealed class SqlitePersistenceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly SafeEcuDbContextFactory _dbContextFactory;
    private readonly NullAppLogger _logger = new();

    public SqlitePersistenceTests()
    {
        _testDirectory = Path.Combine(AppContext.BaseDirectory, "test-data", Guid.NewGuid().ToString("N"));
        var connectionString = SqliteConnectionStringFactory.Create(_testDirectory, "test-safe-ecu.db");
        _dbContextFactory = new SafeEcuDbContextFactory(connectionString);
    }

    [Fact]
    public async Task Initializer_creates_sqlite_database()
    {
        var initializer = new SqlitePersistenceInitializer(_dbContextFactory, _logger);

        var result = await initializer.InitializeAsync();

        Assert.True(result.IsSuccess);
        Assert.True(File.Exists(Path.Combine(_testDirectory, "test-safe-ecu.db")));
    }

    [Fact]
    public async Task Vehicle_service_can_save_and_read_vehicle()
    {
        await InitializeDatabaseAsync();
        var repository = new VehicleRepository(_dbContextFactory);
        var service = new VehicleService(repository, _logger);

        var createResult = await service.CreateAsync(new Vehicle
        {
            Make = "Opel",
            Model = "Corsa C",
            Engine = "1.7 DTI",
            EngineCode = "Y17DT",
            FuelType = FuelType.Diesel
        });

        var vehicles = await service.ListAsync();

        Assert.True(createResult.IsSuccess);
        Assert.Single(vehicles);
        Assert.Equal("Opel", vehicles[0].Make);
        Assert.Equal("Y17DT", vehicles[0].EngineCode);
    }

    [Fact]
    public async Task Can_save_ecu_info_and_file_metadata_for_vehicle()
    {
        await InitializeDatabaseAsync();
        var vehicleRepository = new VehicleRepository(_dbContextFactory);
        var ecuRepository = new EcuInfoRepository(_dbContextFactory);
        var fileRepository = new EcuFileRepository(_dbContextFactory);

        var vehicle = new Vehicle
        {
            Make = "Renault",
            Model = "Megane 3",
            Engine = "1.5 dCi",
            EngineCode = "K9K",
            FuelType = FuelType.Diesel
        };

        await vehicleRepository.AddAsync(vehicle);

        var ecuInfo = new EcuInfo
        {
            VehicleId = vehicle.Id,
            Manufacturer = "Unknown",
            EcuFamily = "Unknown",
            SupportStatus = SupportStatus.Unknown
        };

        await ecuRepository.AddAsync(ecuInfo);

        var ecuFile = new EcuFile
        {
            VehicleId = vehicle.Id,
            EcuInfoId = ecuInfo.Id,
            FileType = EcuFileType.Original,
            FileName = "original.bin",
            FilePath = "backups/original.bin",
            SizeBytes = 1024,
            Sha256Hash = new string('a', 64),
            FileOrigin = "External read",
            ReadMethod = "ManualWorkflowOnly",
            ProgrammerUsed = "Galletto 1260 / EOBD Programmer 1260",
            ChecksumStatus = ChecksumStatus.NotSupported
        };

        await fileRepository.AddAsync(ecuFile);

        var ecus = await ecuRepository.ListByVehicleAsync(vehicle.Id);
        var files = await fileRepository.ListByVehicleAsync(vehicle.Id);

        Assert.Single(ecus);
        Assert.Single(files);
        Assert.Equal(ChecksumStatus.NotSupported, files[0].ChecksumStatus);
    }

    [Fact]
    public async Task Ecu_file_service_rejects_duplicate_sha256_hash()
    {
        await InitializeDatabaseAsync();
        var vehicleRepository = new VehicleRepository(_dbContextFactory);
        var fileRepository = new EcuFileRepository(_dbContextFactory);
        var service = new EcuFileService(fileRepository, _logger);

        var vehicle = new Vehicle { Make = "Opel", Model = "Corsa C" };
        await vehicleRepository.AddAsync(vehicle);

        var first = CreateEcuFile(vehicle.Id, new string('b', 64), "first.bin");
        var duplicate = CreateEcuFile(vehicle.Id, new string('b', 64), "duplicate.bin");

        var firstResult = await service.RegisterMetadataAsync(first);
        var duplicateResult = await service.RegisterMetadataAsync(duplicate);

        Assert.True(firstResult.IsSuccess);
        Assert.False(duplicateResult.IsSuccess);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

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

    private static EcuFile CreateEcuFile(Guid vehicleId, string hash, string fileName) =>
        new()
        {
            VehicleId = vehicleId,
            FileType = EcuFileType.Unknown,
            FileName = fileName,
            FilePath = $"backups/{fileName}",
            SizeBytes = 512,
            Sha256Hash = hash,
            ChecksumStatus = ChecksumStatus.Unknown
        };
}
