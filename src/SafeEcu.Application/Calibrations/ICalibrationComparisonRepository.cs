using SafeEcu.Domain.Calibrations;

namespace SafeEcu.Application.Calibrations;

public interface ICalibrationComparisonRepository
{
    Task AddAsync(CalibrationComparison comparison, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CalibrationComparison>> ListAsync(CancellationToken cancellationToken = default);
}
