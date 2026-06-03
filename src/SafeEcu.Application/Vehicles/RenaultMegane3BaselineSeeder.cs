using SafeEcu.Application.Common;
using SafeEcu.Application.Persistence;
using SafeEcu.Domain.Application;
using SafeEcu.Domain.Vehicles;

namespace SafeEcu.Application.Vehicles;

public sealed class RenaultMegane3BaselineSeeder
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IEcuInfoRepository _ecuInfoRepository;
    private readonly IAppLogger _logger;

    public RenaultMegane3BaselineSeeder(
        IVehicleRepository vehicleRepository,
        IEcuInfoRepository ecuInfoRepository,
        IAppLogger logger)
    {
        _vehicleRepository = vehicleRepository;
        _ecuInfoRepository = ecuInfoRepository;
        _logger = logger;
    }

    public async Task SeedIfEmptyAsync(CancellationToken cancellationToken = default)
    {
        var vehicles = await _vehicleRepository.ListAsync(cancellationToken);
        if (vehicles.Count > 0)
        {
            return;
        }

        var vehicle = new Vehicle
        {
            Make = "Renault",
            Model = "Megane 3",
            Engine = "1.5 dCi",
            EngineCode = "K9K",
            FuelType = FuelType.Diesel,
            Notes = "Baseline Renault Megane 3 target. Exact K9K variant, ECU label, hardware reference, software reference, software version, original file size and SHA-256 hash must be confirmed before any advanced support."
        };

        await _vehicleRepository.AddAsync(vehicle, cancellationToken);
        await _ecuInfoRepository.AddAsync(CreateCandidateEcu(
            vehicle.Id,
            "Delphi",
            "Delphi DCM",
            "Primary Renault K9K candidate family. Exact DCM variant, hardware/software references and checksum support are not verified.",
            cancellationToken), cancellationToken);
        await _ecuInfoRepository.AddAsync(CreateCandidateEcu(
            vehicle.Id,
            "Bosch",
            "Bosch EDC",
            "Secondary Renault K9K candidate family. Exact EDC variant, hardware/software references and checksum support are not verified.",
            cancellationToken), cancellationToken);

        _logger.Information("Renault Megane 3 baseline records seeded.");
    }

    private static EcuInfo CreateCandidateEcu(
        Guid vehicleId,
        string manufacturer,
        string ecuFamily,
        string notes,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return new EcuInfo
        {
            VehicleId = vehicleId,
            Manufacturer = manufacturer,
            EcuFamily = ecuFamily,
            Protocol = "FileOnly / ManualWorkflowOnly",
            SupportStatus = SupportStatus.FileManagement,
            Notes = notes
        };
    }
}
