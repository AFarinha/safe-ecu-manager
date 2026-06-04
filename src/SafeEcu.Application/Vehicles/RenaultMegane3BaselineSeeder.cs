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
        await EnsureRenaultMegane3Async(vehicles, cancellationToken);
        await EnsureOpelCorsaCAsync(vehicles, cancellationToken);
    }

    private async Task EnsureRenaultMegane3Async(
        IReadOnlyList<Vehicle> vehicles,
        CancellationToken cancellationToken)
    {
        var vehicle = vehicles.FirstOrDefault(vehicle =>
            string.Equals(vehicle.Make, "Renault", StringComparison.OrdinalIgnoreCase)
            && string.Equals(vehicle.Model, "Megane 3", StringComparison.OrdinalIgnoreCase));
        if (vehicle is null)
        {
            vehicle = new Vehicle
            {
                Make = "Renault",
                Model = "Megane 3",
                Engine = "1.5 dCi",
                EngineCode = "K9K",
                FuelType = FuelType.Diesel,
                Notes = "Baseline Renault Megane 3 target. Exact K9K variant, ECU label, hardware reference, software reference, software version, original file size and SHA-256 hash must be confirmed before any advanced support."
            };

            await _vehicleRepository.AddAsync(vehicle, cancellationToken);
        }

        await EnsureCandidateEcuAsync(
            vehicle.Id,
            "Delphi",
            "Delphi DCM",
            "Primary Renault K9K candidate family. Exact DCM variant, hardware/software references and checksum support are not verified.",
            cancellationToken);
        await EnsureCandidateEcuAsync(
            vehicle.Id,
            "Bosch",
            "Bosch EDC",
            "Secondary Renault K9K candidate family. Exact EDC variant, hardware/software references and checksum support are not verified.",
            cancellationToken);

        _logger.Information("Renault Megane 3 baseline records seeded.");
    }

    private async Task EnsureOpelCorsaCAsync(
        IReadOnlyList<Vehicle> vehicles,
        CancellationToken cancellationToken)
    {
        var vehicle = vehicles.FirstOrDefault(vehicle =>
            string.Equals(vehicle.Make, "Opel", StringComparison.OrdinalIgnoreCase)
            && string.Equals(vehicle.Model, "Corsa C", StringComparison.OrdinalIgnoreCase));
        if (vehicle is null)
        {
            vehicle = new Vehicle
            {
                Make = "Opel",
                Model = "Corsa C",
                Engine = "1.7 DTI",
                EngineCode = "Y17DT/Y17DTI",
                FuelType = FuelType.Diesel,
                Notes = "Secondary Opel Corsa C 1.7 DTI metadata target. Exact ECU/EDU label, hardware reference, software reference, software version, original file size and SHA-256 hash must be confirmed before any advanced support."
            };

            await _vehicleRepository.AddAsync(vehicle, cancellationToken);
        }

        await EnsureCandidateEcuAsync(
            vehicle.Id,
            "Delco/Delphi/Isuzu",
            "Delco/Delphi/Isuzu Opel 1.7 DTI EDU",
            "Candidate Opel Corsa C 1.7 DTI ECU/EDU family. Exact hardware/software references and checksum support are not verified.",
            cancellationToken);
        await EnsureCandidateEcuAsync(
            vehicle.Id,
            "Bosch",
            "Bosch VP44 PSG5/PSG16",
            "Secondary Opel 1.7 DTI candidate only when confirmed by real vehicle evidence. No direct hardware, boot, bench or recovery support.",
            cancellationToken);

        _logger.Information("Opel Corsa C baseline records seeded.");
    }

    private async Task EnsureCandidateEcuAsync(
        Guid vehicleId,
        string manufacturer,
        string ecuFamily,
        string notes,
        CancellationToken cancellationToken)
    {
        var ecus = await _ecuInfoRepository.ListByVehicleAsync(vehicleId, cancellationToken);
        if (ecus.Any(ecu =>
                string.Equals(ecu.Manufacturer, manufacturer, StringComparison.OrdinalIgnoreCase)
                && string.Equals(ecu.EcuFamily, ecuFamily, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        await _ecuInfoRepository.AddAsync(CreateCandidateEcu(
            vehicleId,
            manufacturer,
            ecuFamily,
            notes,
            cancellationToken), cancellationToken);
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
