using SafeEcu.Domain.Vehicles;

namespace SafeEcu.Application.Checksums;

public sealed class ChecksumAlgorithmRegistry
{
    private readonly IReadOnlyList<IChecksumAlgorithm> _algorithms;

    public ChecksumAlgorithmRegistry(IEnumerable<IChecksumAlgorithm> algorithms)
    {
        _algorithms = algorithms.ToArray();
    }

    public IChecksumAlgorithm? FindFor(EcuInfo ecuInfo) =>
        _algorithms.FirstOrDefault(algorithm => algorithm.Supports(ecuInfo));
}
