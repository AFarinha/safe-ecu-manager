using System.Security.Cryptography;
using SafeEcu.Application.CalibrationPatching;

namespace SafeEcu.Application.MapWorkspace;

public sealed class MapChangeExportService
{
    public async Task<MapChangeExportResult> ExportAsync(
        MapChangeExportRequest request,
        CancellationToken cancellationToken = default)
    {
        var blockReasons = Validate(request);
        if (blockReasons.Count > 0)
        {
            return new MapChangeExportResult(false, null, null, [], blockReasons);
        }

        var bytes = await File.ReadAllBytesAsync(request.OriginalFilePath, cancellationToken);
        var mapsById = request.MapDefinitions.ToDictionary(map => map.MapId, StringComparer.OrdinalIgnoreCase);

        foreach (var change in request.Changes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var map = mapsById[change.MapId];
            var absoluteOffset = map.AddressRange.StartOffset + change.RelativeOffset;
            bytes[(int)absoluteOffset] = change.ProposedValue;
        }

        Directory.CreateDirectory(request.OutputDirectory);
        var outputPath = Path.Combine(request.OutputDirectory, request.OutputFileName);
        await File.WriteAllBytesAsync(outputPath, bytes, cancellationToken);
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

        return new MapChangeExportResult(
            true,
            outputPath,
            hash,
            [$"Modified calibration file exported to '{outputPath}'."],
            []);
    }

    private static List<string> Validate(MapChangeExportRequest request)
    {
        var blockReasons = new List<string>();
        if (!request.ExportGate.CanExport)
        {
            blockReasons.AddRange(request.ExportGate.BlockReasons);
            blockReasons.Add("Export gate did not pass.");
        }

        if (!request.ChecksumRecalculated)
        {
            blockReasons.Add("Checksum must be recalculated before exporting a modified calibration file.");
        }

        if (string.IsNullOrWhiteSpace(request.OriginalFilePath) || !File.Exists(request.OriginalFilePath))
        {
            blockReasons.Add("Original ECU file could not be found.");
        }

        if (string.IsNullOrWhiteSpace(request.OutputDirectory))
        {
            blockReasons.Add("Output directory is required.");
        }

        if (string.IsNullOrWhiteSpace(request.OutputFileName))
        {
            blockReasons.Add("Output file name is required.");
        }

        if (request.Changes.Count == 0)
        {
            blockReasons.Add("At least one validated map change is required for export.");
        }

        var mapsById = request.MapDefinitions.ToDictionary(map => map.MapId, StringComparer.OrdinalIgnoreCase);
        foreach (var change in request.Changes)
        {
            if (!mapsById.TryGetValue(change.MapId, out var map))
            {
                blockReasons.Add($"Map '{change.MapId}' was not found for export.");
                continue;
            }

            if (!IsDummyOrTestMap(map))
            {
                blockReasons.Add($"Map '{map.MapId}' is not from a dummy/test profile. Real ECU export remains blocked.");
            }

            if (map.SupportStatus != CalibrationMapDefinitionSupportStatus.Verified)
            {
                blockReasons.Add($"Map '{map.MapId}' is not verified.");
            }

            if (map.IsEmissionsRelated)
            {
                blockReasons.Add($"Map '{map.MapId}' is emissions-related and cannot be exported.");
            }

            var absoluteOffset = map.AddressRange.StartOffset + change.RelativeOffset;
            if (change.RelativeOffset < 0 || absoluteOffset > map.AddressRange.EndOffset)
            {
                blockReasons.Add($"Change for map '{map.MapId}' is outside the map range.");
            }

            if (File.Exists(request.OriginalFilePath))
            {
                var fileSize = new FileInfo(request.OriginalFilePath).Length;
                if (absoluteOffset < 0 || absoluteOffset >= fileSize)
                {
                    blockReasons.Add($"Change for map '{map.MapId}' is outside the original file.");
                }
            }
        }

        return blockReasons.Distinct().ToList();
    }

    private static bool IsDummyOrTestMap(CalibrationMapDefinition map) =>
        map.ProfileId.Contains("dummy", StringComparison.OrdinalIgnoreCase)
        || map.ProfileId.Contains("test", StringComparison.OrdinalIgnoreCase);
}
