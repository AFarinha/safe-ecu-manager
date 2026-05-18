using SafeEcu.Application.Common;
using SafeEcu.Application.Persistence;
using SafeEcu.Domain.Vehicles;

namespace SafeEcu.Application.Vehicles;

public sealed class EcuInfoService
{
    private readonly IEcuInfoRepository _ecuInfoRepository;
    private readonly IAppLogger _logger;

    public EcuInfoService(IEcuInfoRepository ecuInfoRepository, IAppLogger logger)
    {
        _ecuInfoRepository = ecuInfoRepository;
        _logger = logger;
    }

    public async Task<OperationResult<EcuInfo>> CreateAsync(
        EcuInfo ecuInfo,
        CancellationToken cancellationToken = default)
    {
        if (ecuInfo.VehicleId == Guid.Empty)
        {
            return OperationResult<EcuInfo>.Failure("The ECU/EDU must be associated with a vehicle.");
        }

        ecuInfo.CreatedAt = DateTimeOffset.UtcNow;
        ecuInfo.UpdatedAt = ecuInfo.CreatedAt;

        await _ecuInfoRepository.AddAsync(ecuInfo, cancellationToken);
        _logger.Information($"ECU info created: {ecuInfo.Id}.");

        return OperationResult<EcuInfo>.Success(ecuInfo);
    }
}
