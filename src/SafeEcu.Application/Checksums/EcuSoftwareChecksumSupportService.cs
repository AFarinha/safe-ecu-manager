using SafeEcu.Application.EcuProfiles;

namespace SafeEcu.Application.Checksums;

public sealed class EcuSoftwareChecksumSupportService
{
    private readonly EcuSoftwareChecksumRegistry _registry;

    public EcuSoftwareChecksumSupportService(EcuSoftwareChecksumRegistry registry)
    {
        _registry = registry;
    }

    public EcuSoftwareChecksumSupportResult Evaluate(VerifiedEcuSoftwareProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.ChecksumAlgorithmId))
        {
            return Block("None", "Verified ECU software profile does not define a checksum algorithm id.");
        }

        var algorithm = _registry.FindFor(profile);
        if (algorithm is null)
        {
            return Block(
                profile.ChecksumAlgorithmId,
                $"Checksum algorithm '{profile.ChecksumAlgorithmId}' is not registered for this ECU/software profile.");
        }

        return new EcuSoftwareChecksumSupportResult(
            true,
            true,
            ChecksumValidationStatus.Valid,
            algorithm.AlgorithmId,
            [$"Checksum algorithm '{algorithm.AlgorithmId}' is registered for this ECU/software profile."],
            []);
    }

    private static EcuSoftwareChecksumSupportResult Block(string algorithmId, string reason) =>
        new(
            false,
            false,
            ChecksumValidationStatus.NotSupported,
            algorithmId,
            [],
            [reason]);
}
