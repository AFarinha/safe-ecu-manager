using Microsoft.EntityFrameworkCore;

namespace SafeEcu.Infrastructure.Persistence;

public sealed class SafeEcuDbContextFactory
{
    private readonly string _connectionString;

    public SafeEcuDbContextFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public SafeEcuDbContext Create()
    {
        var options = new DbContextOptionsBuilder<SafeEcuDbContext>()
            .UseSqlite(_connectionString)
            .Options;

        return new SafeEcuDbContext(options);
    }
}
