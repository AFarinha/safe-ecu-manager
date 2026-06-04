using Microsoft.EntityFrameworkCore;
using SafeEcu.Domain.Auditing;
using SafeEcu.Domain.Calibrations;
using SafeEcu.Domain.Projects;
using SafeEcu.Domain.Vehicles;

namespace SafeEcu.Infrastructure.Persistence;

public sealed class SafeEcuDbContext : DbContext
{
    public SafeEcuDbContext(DbContextOptions<SafeEcuDbContext> options)
        : base(options)
    {
    }

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    public DbSet<EcuInfo> EcuInfos => Set<EcuInfo>();

    public DbSet<EcuFile> EcuFiles => Set<EcuFile>();

    public DbSet<CalibrationComparison> CalibrationComparisons => Set<CalibrationComparison>();

    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

    public DbSet<SafetyLimit> SafetyLimits => Set<SafetyLimit>();

    public DbSet<EcuProject> EcuProjects => Set<EcuProject>();

    public DbSet<EcuProjectFileVersion> EcuProjectFileVersions => Set<EcuProjectFileVersion>();

    public DbSet<EcuProjectMapDefinition> EcuProjectMapDefinitions => Set<EcuProjectMapDefinition>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureVehicle(modelBuilder);
        ConfigureEcuInfo(modelBuilder);
        ConfigureEcuFile(modelBuilder);
        ConfigureCalibrationComparison(modelBuilder);
        ConfigureAuditLogEntry(modelBuilder);
        ConfigureSafetyLimit(modelBuilder);
        ConfigureEcuProject(modelBuilder);
        ConfigureEcuProjectFileVersion(modelBuilder);
        ConfigureEcuProjectMapDefinition(modelBuilder);
    }

    private static void ConfigureVehicle(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Vehicle>();

        entity.HasKey(vehicle => vehicle.Id);
        entity.Property(vehicle => vehicle.Vin).HasMaxLength(32);
        entity.Property(vehicle => vehicle.LicensePlate).HasMaxLength(32);
        entity.Property(vehicle => vehicle.Make).HasMaxLength(80).IsRequired();
        entity.Property(vehicle => vehicle.Model).HasMaxLength(120).IsRequired();
        entity.Property(vehicle => vehicle.Engine).HasMaxLength(120);
        entity.Property(vehicle => vehicle.EngineCode).HasMaxLength(80);
        entity.Property(vehicle => vehicle.Notes).HasMaxLength(2000);

        entity.HasIndex(vehicle => vehicle.Vin);
        entity.HasIndex(vehicle => vehicle.LicensePlate);
    }

    private static void ConfigureEcuInfo(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<EcuInfo>();

        entity.HasKey(ecu => ecu.Id);
        entity.Property(ecu => ecu.Manufacturer).HasMaxLength(120);
        entity.Property(ecu => ecu.EcuFamily).HasMaxLength(120);
        entity.Property(ecu => ecu.HardwareReference).HasMaxLength(160);
        entity.Property(ecu => ecu.SoftwareReference).HasMaxLength(160);
        entity.Property(ecu => ecu.SoftwareVersion).HasMaxLength(120);
        entity.Property(ecu => ecu.Protocol).HasMaxLength(80);
        entity.Property(ecu => ecu.Notes).HasMaxLength(2000);

        entity
            .HasOne(ecu => ecu.Vehicle)
            .WithMany(vehicle => vehicle.Ecus)
            .HasForeignKey(ecu => ecu.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(ecu => ecu.VehicleId);
    }

    private static void ConfigureEcuFile(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<EcuFile>();

        entity.HasKey(file => file.Id);
        entity.Property(file => file.FileName).HasMaxLength(260).IsRequired();
        entity.Property(file => file.FilePath).HasMaxLength(1024).IsRequired();
        entity.Property(file => file.Sha256Hash).HasMaxLength(64).IsRequired();
        entity.Property(file => file.FileOrigin).HasMaxLength(200);
        entity.Property(file => file.ReadMethod).HasMaxLength(160);
        entity.Property(file => file.ProgrammerUsed).HasMaxLength(160);
        entity.Property(file => file.Notes).HasMaxLength(2000);

        entity
            .HasOne(file => file.Vehicle)
            .WithMany(vehicle => vehicle.EcuFiles)
            .HasForeignKey(file => file.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasOne(file => file.EcuInfo)
            .WithMany(ecu => ecu.EcuFiles)
            .HasForeignKey(file => file.EcuInfoId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(file => file.VehicleId);
        entity.HasIndex(file => file.EcuInfoId);
        entity.HasIndex(file => file.Sha256Hash).IsUnique();
    }

    private static void ConfigureCalibrationComparison(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<CalibrationComparison>();

        entity.HasKey(comparison => comparison.Id);
        entity.Property(comparison => comparison.Result).HasMaxLength(80).IsRequired();
        entity.Property(comparison => comparison.DifferenceSummary).HasMaxLength(4000);
        entity.Property(comparison => comparison.DifferenceBlockSummary).HasMaxLength(4000);
        entity.Property(comparison => comparison.PercentChanged).HasColumnType("decimal(18,6)");

        entity
            .HasOne(comparison => comparison.OriginalFile)
            .WithMany()
            .HasForeignKey(comparison => comparison.OriginalFileId)
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasOne(comparison => comparison.ModifiedFile)
            .WithMany()
            .HasForeignKey(comparison => comparison.ModifiedFileId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(comparison => comparison.OriginalFileId);
        entity.HasIndex(comparison => comparison.ModifiedFileId);
        entity.HasIndex(comparison => comparison.ComparedAt);
    }

    private static void ConfigureAuditLogEntry(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<AuditLogEntry>();

        entity.HasKey(entry => entry.Id);
        entity.Property(entry => entry.Action).HasMaxLength(120).IsRequired();
        entity.Property(entry => entry.EntityType).HasMaxLength(120).IsRequired();
        entity.Property(entry => entry.EntityId).HasMaxLength(120);
        entity.Property(entry => entry.Severity).HasMaxLength(40).IsRequired();
        entity.Property(entry => entry.Message).HasMaxLength(1000).IsRequired();
        entity.Property(entry => entry.Details).HasMaxLength(4000);
        entity.Property(entry => entry.UserOrMachineName).HasMaxLength(160);

        entity.HasIndex(entry => entry.Timestamp);
        entity.HasIndex(entry => entry.CorrelationId);
        entity.HasIndex(entry => new { entry.EntityType, entry.EntityId });
    }

    private static void ConfigureSafetyLimit(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<SafetyLimit>();

        entity.HasKey(limit => limit.Id);
        entity.Property(limit => limit.EcuFamily).HasMaxLength(120);
        entity.Property(limit => limit.SoftwareVersion).HasMaxLength(120);
        entity.Property(limit => limit.EngineCode).HasMaxLength(80);
        entity.Property(limit => limit.ProfileId).HasMaxLength(160);
        entity.Property(limit => limit.MapId).HasMaxLength(160);
        entity.Property(limit => limit.ParameterName).HasMaxLength(160).IsRequired();
        entity.Property(limit => limit.Unit).HasMaxLength(40);
        entity.Property(limit => limit.Severity).HasMaxLength(40);
        entity.Property(limit => limit.Reason).HasMaxLength(1000);
        entity.Property(limit => limit.SafetyStatus).HasMaxLength(80);
        entity.Property(limit => limit.Source).HasMaxLength(500);
        entity.Property(limit => limit.Notes).HasMaxLength(2000);
        entity.Property(limit => limit.MinValue).HasColumnType("decimal(18,6)");
        entity.Property(limit => limit.MaxValue).HasColumnType("decimal(18,6)");

        entity.HasIndex(limit => limit.ProfileId);
        entity.HasIndex(limit => new { limit.ProfileId, limit.ParameterName });
    }

    private static void ConfigureEcuProject(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<EcuProject>();

        entity.HasKey(project => project.Id);
        entity.Property(project => project.Name).HasMaxLength(160).IsRequired();
        entity.Property(project => project.Make).HasMaxLength(80);
        entity.Property(project => project.Model).HasMaxLength(120);
        entity.Property(project => project.Engine).HasMaxLength(120);
        entity.Property(project => project.EcuType).HasMaxLength(120);
        entity.Property(project => project.EcuReference).HasMaxLength(160);
        entity.Property(project => project.ReadType).HasMaxLength(80);
        entity.Property(project => project.Notes).HasMaxLength(2000);
        entity.Property(project => project.Tags).HasMaxLength(500);

        entity
            .HasOne(project => project.Vehicle)
            .WithMany()
            .HasForeignKey(project => project.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasOne(project => project.EcuInfo)
            .WithMany()
            .HasForeignKey(project => project.EcuInfoId)
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasOne(project => project.OriginalFile)
            .WithMany()
            .HasForeignKey(project => project.OriginalFileId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(project => project.VehicleId);
        entity.HasIndex(project => project.OriginalFileId);
        entity.HasIndex(project => project.UpdatedAt);
    }

    private static void ConfigureEcuProjectFileVersion(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<EcuProjectFileVersion>();

        entity.HasKey(version => version.Id);
        entity.Property(version => version.Label).HasMaxLength(160).IsRequired();
        entity.Property(version => version.Notes).HasMaxLength(2000);

        entity
            .HasOne(version => version.Project)
            .WithMany(project => project.Versions)
            .HasForeignKey(version => version.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        entity
            .HasOne(version => version.EcuFile)
            .WithMany()
            .HasForeignKey(version => version.EcuFileId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(version => version.ProjectId);
        entity.HasIndex(version => version.EcuFileId);
        entity.HasIndex(version => new { version.ProjectId, version.VersionNumber }).IsUnique();
    }

    private static void ConfigureEcuProjectMapDefinition(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<EcuProjectMapDefinition>();

        entity.HasKey(definition => definition.Id);
        entity.Property(definition => definition.MapId).HasMaxLength(160).IsRequired();
        entity.Property(definition => definition.DisplayName).HasMaxLength(160).IsRequired();
        entity.Property(definition => definition.ParameterName).HasMaxLength(160).IsRequired();
        entity.Property(definition => definition.Category).HasMaxLength(80);
        entity.Property(definition => definition.DataType).HasMaxLength(40);
        entity.Property(definition => definition.Endianess).HasMaxLength(40);
        entity.Property(definition => definition.Unit).HasMaxLength(40);
        entity.Property(definition => definition.XAxisUnit).HasMaxLength(40);
        entity.Property(definition => definition.YAxisUnit).HasMaxLength(40);
        entity.Property(definition => definition.RequiredMapIds).HasMaxLength(1000);
        entity.Property(definition => definition.ProfileId).HasMaxLength(160);
        entity.Property(definition => definition.SupportStatus).HasMaxLength(40);
        entity.Property(definition => definition.Notes).HasMaxLength(2000);
        entity.Property(definition => definition.Factor).HasColumnType("decimal(18,6)");
        entity.Property(definition => definition.OffsetCorrection).HasColumnType("decimal(18,6)");
        entity.Property(definition => definition.MinimumValue).HasColumnType("decimal(18,6)");
        entity.Property(definition => definition.MaximumValue).HasColumnType("decimal(18,6)");

        entity
            .HasOne(definition => definition.Project)
            .WithMany(project => project.MapDefinitions)
            .HasForeignKey(definition => definition.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(definition => definition.ProjectId);
        entity.HasIndex(definition => new { definition.ProjectId, definition.MapId }).IsUnique();
        entity.HasIndex(definition => definition.Category);
    }
}
