using Microsoft.EntityFrameworkCore;
using SafeEcu.Domain.Auditing;
using SafeEcu.Domain.Calibrations;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureVehicle(modelBuilder);
        ConfigureEcuInfo(modelBuilder);
        ConfigureEcuFile(modelBuilder);
        ConfigureCalibrationComparison(modelBuilder);
        ConfigureAuditLogEntry(modelBuilder);
        ConfigureSafetyLimit(modelBuilder);
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
}
