using SafeEcu.Domain.Vehicles;

namespace SafeEcu.Application.Vehicles;

public sealed class VehicleProfileCatalog
{
    public IReadOnlyList<VehicleProfile> Profiles { get; } =
    [
        new(
            "opel-corsa-c-y17dti",
            "Opel Corsa C 1.7 DTI Y17DT/Y17DTI",
            "Opel",
            "Corsa C",
            "1.7 DTI",
            "Y17DT/Y17DTI",
            FuelType.Diesel,
            "Initial target profile. ECU/EDU reference, software version, file size and original hash must be confirmed manually before any advanced support."),
        new(
            "renault-megane-3-k9k",
            "Renault Megane 3 1.5 dCi K9K",
            "Renault",
            "Megane 3",
            "1.5 dCi",
            "K9K",
            FuelType.Diesel,
            "Future target profile. Exact ECU family, software version, file size and original hash must be confirmed manually before any advanced support.")
    ];
}
