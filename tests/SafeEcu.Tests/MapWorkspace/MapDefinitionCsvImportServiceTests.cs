using SafeEcu.Application.CalibrationPatching;
using SafeEcu.Application.MapWorkspace;

namespace SafeEcu.Tests.MapWorkspace;

public sealed class MapDefinitionCsvImportServiceTests : IDisposable
{
    private readonly string _testDirectory = Path.Combine(
        AppContext.BaseDirectory,
        "map-definition-csv-import-tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task ImportAsync_parses_map_definitions_from_csv()
    {
        Directory.CreateDirectory(_testDirectory);
        var csvPath = Path.Combine(_testDirectory, "maps.csv");
        await File.WriteAllTextAsync(
            csvPath,
            """
            MapId,DisplayName,ParameterName,StartOffset,Length,DataType,Endianess,Factor,Unit,MinimumValue,MaximumValue,IsEmissionsRelated,RequiredMapIds,ProfileId,SupportStatus
            TorqueLimiter,Torque limiter,Torque,0x0010,8,UInt8,NotApplicable,1,Nm,0,255,false,SmokeLimiter;RailPressure,dummy-test-profile,Verified
            """);
        var service = new MapDefinitionCsvImportService();

        var result = await service.ImportAsync(csvPath);

        Assert.True(result.IsSuccess);
        var map = Assert.Single(result.MapDefinitions);
        Assert.Equal("TorqueLimiter", map.MapId);
        Assert.Equal(0x10, map.AddressRange.StartOffset);
        Assert.Equal(8, map.AddressRange.Length);
        Assert.Equal(CalibrationMapDataType.UInt8, map.DataType);
        Assert.Equal(CalibrationMapEndianess.NotApplicable, map.Endianess);
        Assert.Equal(1m, map.Factor);
        Assert.Equal("Nm", map.Unit);
        Assert.False(map.IsEmissionsRelated);
        Assert.Equal(["SmokeLimiter", "RailPressure"], map.RequiredMapIds);
        Assert.Equal(CalibrationMapDefinitionSupportStatus.Verified, map.SupportStatus);
    }

    [Fact]
    public async Task ImportAsync_rejects_missing_required_headers()
    {
        Directory.CreateDirectory(_testDirectory);
        var csvPath = Path.Combine(_testDirectory, "maps.csv");
        await File.WriteAllTextAsync(csvPath, "MapId,DisplayName\nFuel,Fuel map\n");
        var service = new MapDefinitionCsvImportService();

        var result = await service.ImportAsync(csvPath);

        Assert.False(result.IsSuccess);
        Assert.Empty(result.MapDefinitions);
        Assert.Contains(result.Errors, error => error.Contains("missing headers", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ImportAsync_rejects_invalid_rows()
    {
        Directory.CreateDirectory(_testDirectory);
        var csvPath = Path.Combine(_testDirectory, "maps.csv");
        await File.WriteAllTextAsync(
            csvPath,
            """
            MapId,DisplayName,ParameterName,StartOffset,Length,DataType,Endianess,Factor,Unit,MinimumValue,MaximumValue,IsEmissionsRelated,RequiredMapIds,ProfileId,SupportStatus
            Fuel,Fuel quantity,Fuel,not-a-number,8,UInt8,NotApplicable,1,mg,,,false,,dummy-test-profile,Verified
            """);
        var service = new MapDefinitionCsvImportService();

        var result = await service.ImportAsync(csvPath);

        Assert.False(result.IsSuccess);
        Assert.Empty(result.MapDefinitions);
        Assert.Contains(result.Errors, error => error.Contains("Line 2", StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }
}
