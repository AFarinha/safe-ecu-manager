using SafeEcu.Application.Common;
using SafeEcu.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace SafeEcu.Infrastructure.Persistence;

public sealed class SqlitePersistenceInitializer : IPersistenceInitializer
{
    private readonly SafeEcuDbContextFactory _dbContextFactory;
    private readonly IAppLogger _logger;

    public SqlitePersistenceInitializer(SafeEcuDbContextFactory dbContextFactory, IAppLogger logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task<OperationResult> InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var dbContext = _dbContextFactory.Create();
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
            await EnsureCalibrationComparisonTableAsync(dbContext, cancellationToken);
            await EnsureAuditLogTableAsync(dbContext, cancellationToken);
            await EnsureSafetyLimitTableAsync(dbContext, cancellationToken);
            _logger.Information("SQLite database initialized.");

            return OperationResult.Success();
        }
        catch (Exception exception)
        {
            _logger.Error("Failed to initialize SQLite database.", exception);
            return OperationResult.Failure("The local database could not be initialized.");
        }
    }

    private static Task EnsureCalibrationComparisonTableAsync(
        SafeEcuDbContext dbContext,
        CancellationToken cancellationToken) =>
        dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "CalibrationComparisons" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_CalibrationComparisons" PRIMARY KEY,
                "OriginalFileId" TEXT NOT NULL,
                "ModifiedFileId" TEXT NOT NULL,
                "DifferenceCount" INTEGER NOT NULL,
                "PercentChanged" TEXT NOT NULL,
                "Result" TEXT NOT NULL,
                "ComparedAt" TEXT NOT NULL,
                CONSTRAINT "FK_CalibrationComparisons_EcuFiles_OriginalFileId" FOREIGN KEY ("OriginalFileId") REFERENCES "EcuFiles" ("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_CalibrationComparisons_EcuFiles_ModifiedFileId" FOREIGN KEY ("ModifiedFileId") REFERENCES "EcuFiles" ("Id") ON DELETE RESTRICT
            );
            CREATE INDEX IF NOT EXISTS "IX_CalibrationComparisons_OriginalFileId" ON "CalibrationComparisons" ("OriginalFileId");
            CREATE INDEX IF NOT EXISTS "IX_CalibrationComparisons_ModifiedFileId" ON "CalibrationComparisons" ("ModifiedFileId");
            CREATE INDEX IF NOT EXISTS "IX_CalibrationComparisons_ComparedAt" ON "CalibrationComparisons" ("ComparedAt");
            """,
            cancellationToken);

    private static Task EnsureAuditLogTableAsync(
        SafeEcuDbContext dbContext,
        CancellationToken cancellationToken) =>
        dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "AuditLogEntries" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_AuditLogEntries" PRIMARY KEY,
                "Action" TEXT NOT NULL,
                "EntityType" TEXT NOT NULL,
                "EntityId" TEXT NOT NULL,
                "Timestamp" TEXT NOT NULL,
                "Severity" TEXT NOT NULL,
                "Message" TEXT NOT NULL,
                "Details" TEXT NOT NULL,
                "UserOrMachineName" TEXT NOT NULL,
                "CorrelationId" TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS "IX_AuditLogEntries_Timestamp" ON "AuditLogEntries" ("Timestamp");
            CREATE INDEX IF NOT EXISTS "IX_AuditLogEntries_CorrelationId" ON "AuditLogEntries" ("CorrelationId");
            CREATE INDEX IF NOT EXISTS "IX_AuditLogEntries_EntityType_EntityId" ON "AuditLogEntries" ("EntityType", "EntityId");
            """,
            cancellationToken);

    private static Task EnsureSafetyLimitTableAsync(
        SafeEcuDbContext dbContext,
        CancellationToken cancellationToken) =>
        dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "SafetyLimits" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_SafetyLimits" PRIMARY KEY,
                "EcuFamily" TEXT NOT NULL,
                "SoftwareVersion" TEXT NOT NULL,
                "EngineCode" TEXT NOT NULL,
                "ProfileId" TEXT NOT NULL,
                "MapId" TEXT NOT NULL,
                "ParameterName" TEXT NOT NULL,
                "MinValue" TEXT NOT NULL,
                "MaxValue" TEXT NOT NULL,
                "Unit" TEXT NOT NULL,
                "Severity" TEXT NOT NULL,
                "Reason" TEXT NOT NULL,
                "SafetyStatus" TEXT NOT NULL,
                "Source" TEXT NOT NULL,
                "Notes" TEXT NOT NULL,
                "CreatedAt" TEXT NOT NULL,
                "UpdatedAt" TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS "IX_SafetyLimits_ProfileId" ON "SafetyLimits" ("ProfileId");
            CREATE INDEX IF NOT EXISTS "IX_SafetyLimits_ProfileId_ParameterName" ON "SafetyLimits" ("ProfileId", "ParameterName");
            """,
            cancellationToken);
}
