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
            await EnsureCalibrationComparisonColumnsAsync(dbContext, cancellationToken);
            await EnsureAuditLogTableAsync(dbContext, cancellationToken);
            await EnsureSafetyLimitTableAsync(dbContext, cancellationToken);
            await EnsureEcuProjectTablesAsync(dbContext, cancellationToken);
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
                "DifferenceSummary" TEXT NOT NULL DEFAULT '',
                "DifferenceBlockSummary" TEXT NOT NULL DEFAULT '',
                "ComparedAt" TEXT NOT NULL,
                CONSTRAINT "FK_CalibrationComparisons_EcuFiles_OriginalFileId" FOREIGN KEY ("OriginalFileId") REFERENCES "EcuFiles" ("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_CalibrationComparisons_EcuFiles_ModifiedFileId" FOREIGN KEY ("ModifiedFileId") REFERENCES "EcuFiles" ("Id") ON DELETE RESTRICT
            );
            CREATE INDEX IF NOT EXISTS "IX_CalibrationComparisons_OriginalFileId" ON "CalibrationComparisons" ("OriginalFileId");
            CREATE INDEX IF NOT EXISTS "IX_CalibrationComparisons_ModifiedFileId" ON "CalibrationComparisons" ("ModifiedFileId");
            CREATE INDEX IF NOT EXISTS "IX_CalibrationComparisons_ComparedAt" ON "CalibrationComparisons" ("ComparedAt");
            """,
            cancellationToken);

    private static async Task EnsureCalibrationComparisonColumnsAsync(
        SafeEcuDbContext dbContext,
        CancellationToken cancellationToken)
    {
        await EnsureColumnAsync(
            dbContext,
            "CalibrationComparisons",
            "DifferenceSummary",
            """ALTER TABLE "CalibrationComparisons" ADD COLUMN "DifferenceSummary" TEXT NOT NULL DEFAULT ''""",
            cancellationToken);
        await EnsureColumnAsync(
            dbContext,
            "CalibrationComparisons",
            "DifferenceBlockSummary",
            """ALTER TABLE "CalibrationComparisons" ADD COLUMN "DifferenceBlockSummary" TEXT NOT NULL DEFAULT ''""",
            cancellationToken);
    }

    private static async Task EnsureColumnAsync(
        SafeEcuDbContext dbContext,
        string tableName,
        string columnName,
        string alterSql,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{tableName}\")";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        await dbContext.Database.ExecuteSqlRawAsync(alterSql, cancellationToken);
    }

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

    private static Task EnsureEcuProjectTablesAsync(
        SafeEcuDbContext dbContext,
        CancellationToken cancellationToken) =>
        dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "EcuProjects" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_EcuProjects" PRIMARY KEY,
                "VehicleId" TEXT NOT NULL,
                "EcuInfoId" TEXT NULL,
                "OriginalFileId" TEXT NOT NULL,
                "Name" TEXT NOT NULL,
                "Make" TEXT NOT NULL,
                "Model" TEXT NOT NULL,
                "Engine" TEXT NOT NULL,
                "Year" INTEGER NULL,
                "EcuType" TEXT NOT NULL,
                "EcuReference" TEXT NULL,
                "ReadType" TEXT NOT NULL,
                "Notes" TEXT NULL,
                "Tags" TEXT NULL,
                "CreatedAt" TEXT NOT NULL,
                "UpdatedAt" TEXT NOT NULL,
                CONSTRAINT "FK_EcuProjects_Vehicles_VehicleId" FOREIGN KEY ("VehicleId") REFERENCES "Vehicles" ("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_EcuProjects_EcuInfos_EcuInfoId" FOREIGN KEY ("EcuInfoId") REFERENCES "EcuInfos" ("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_EcuProjects_EcuFiles_OriginalFileId" FOREIGN KEY ("OriginalFileId") REFERENCES "EcuFiles" ("Id") ON DELETE RESTRICT
            );
            CREATE TABLE IF NOT EXISTS "EcuProjectFileVersions" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_EcuProjectFileVersions" PRIMARY KEY,
                "ProjectId" TEXT NOT NULL,
                "EcuFileId" TEXT NOT NULL,
                "Kind" INTEGER NOT NULL,
                "VersionNumber" INTEGER NOT NULL,
                "Label" TEXT NOT NULL,
                "IsOriginalProtected" INTEGER NOT NULL,
                "Notes" TEXT NULL,
                "CreatedAt" TEXT NOT NULL,
                CONSTRAINT "FK_EcuProjectFileVersions_EcuProjects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "EcuProjects" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_EcuProjectFileVersions_EcuFiles_EcuFileId" FOREIGN KEY ("EcuFileId") REFERENCES "EcuFiles" ("Id") ON DELETE RESTRICT
            );
            CREATE INDEX IF NOT EXISTS "IX_EcuProjects_VehicleId" ON "EcuProjects" ("VehicleId");
            CREATE INDEX IF NOT EXISTS "IX_EcuProjects_OriginalFileId" ON "EcuProjects" ("OriginalFileId");
            CREATE INDEX IF NOT EXISTS "IX_EcuProjects_UpdatedAt" ON "EcuProjects" ("UpdatedAt");
            CREATE INDEX IF NOT EXISTS "IX_EcuProjectFileVersions_ProjectId" ON "EcuProjectFileVersions" ("ProjectId");
            CREATE INDEX IF NOT EXISTS "IX_EcuProjectFileVersions_EcuFileId" ON "EcuProjectFileVersions" ("EcuFileId");
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_EcuProjectFileVersions_ProjectId_VersionNumber" ON "EcuProjectFileVersions" ("ProjectId", "VersionNumber");
            """,
            cancellationToken);
}
