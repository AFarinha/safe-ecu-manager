using Microsoft.EntityFrameworkCore;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureVehicle(modelBuilder);
        ConfigureEcuInfo(modelBuilder);
        ConfigureEcuFile(modelBuilder);
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
}
