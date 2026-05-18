using Microsoft.EntityFrameworkCore;
using SafeEcu.Application.Calibrations;
using SafeEcu.Domain.Calibrations;

namespace SafeEcu.Infrastructure.Persistence.Repositories;

public sealed class CalibrationComparisonRepository : ICalibrationComparisonRepository
{
    private readonly SafeEcuDbContextFactory _dbContextFactory;

    public CalibrationComparisonRepository(SafeEcuDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task AddAsync(CalibrationComparison comparison, CancellationToken cancellationToken = default)
    {
        await using var dbContext = _dbContextFactory.Create();
        dbContext.CalibrationComparisons.Add(comparison);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CalibrationComparison>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = _dbContextFactory.Create();

        var comparisons = await dbContext.CalibrationComparisons
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return comparisons
            .OrderByDescending(comparison => comparison.ComparedAt)
            .ToList();
    }
}
