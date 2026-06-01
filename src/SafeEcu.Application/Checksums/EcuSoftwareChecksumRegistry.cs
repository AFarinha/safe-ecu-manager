using SafeEcu.Application.EcuProfiles;

namespace SafeEcu.Application.Checksums;

public sealed class EcuSoftwareChecksumRegistry
{
    private readonly IReadOnlyList<IEcuSoftwareChecksumAlgorithm> _algorithms;

    public EcuSoftwareChecksumRegistry(IEnumerable<IEcuSoftwareChecksumAlgorithm> algorithms)
    {
        _algorithms = algorithms.ToArray();
    }

    public IEcuSoftwareChecksumAlgorithm? FindFor(VerifiedEcuSoftwareProfile profile) =>
        _algorithms.FirstOrDefault(algorithm =>
            string.Equals(algorithm.AlgorithmId, profile.ChecksumAlgorithmId, StringComparison.OrdinalIgnoreCase)
            && algorithm.Supports(profile));
}
