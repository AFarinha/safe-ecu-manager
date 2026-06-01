using SafeEcu.Application.EcuProfiles;

namespace SafeEcu.Application.Checksums;

public interface IEcuSoftwareChecksumAlgorithm
{
    string AlgorithmId { get; }

    bool Supports(VerifiedEcuSoftwareProfile profile);
}
