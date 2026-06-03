using SafeEcu.Domain.Vehicles;

namespace SafeEcu.Application.Vehicles;

public sealed class VehicleProfileCatalog
{
    public IReadOnlyList<VehicleProfile> Profiles { get; } =
    [
        new(
            "renault-megane-3-k9k",
            "Renault Megane 3 1.5 dCi K9K",
            "Renault",
            "Megane 3",
            "1.5 dCi",
            "K9K",
            FuelType.Diesel,
            "Primary target profile. Exact K9K variant, ECU family, software version, file size and original hash must be confirmed manually before any advanced support."),
        new(
            "opel-corsa-c-y17dti",
            "Opel Corsa C 1.7 DTI Y17DT/Y17DTI",
            "Opel",
            "Corsa C",
            "1.7 DTI",
            "Y17DT/Y17DTI",
            FuelType.Diesel,
            "Secondary metadata profile. ECU/EDU reference, software version, file size and original hash must be confirmed manually before any advanced support.")
    ];
}
