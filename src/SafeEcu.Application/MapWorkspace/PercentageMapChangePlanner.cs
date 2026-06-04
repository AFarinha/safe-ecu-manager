using SafeEcu.Application.CalibrationPatching;

namespace SafeEcu.Application.MapWorkspace;

public sealed class PercentageMapChangePlanner
{
    public async Task<PercentageMapChangePlanResult> PlanAsync(
        PercentageMapChangePlanRequest request,
        CancellationToken cancellationToken = default)
    {
        var blockReasons = Validate(request);
        if (blockReasons.Count > 0)
        {
            return new PercentageMapChangePlanResult(false, [], [], blockReasons);
        }

        var changes = new List<CalibrationChange>();
        var multiplier = 1m + request.RequestedPercentage / 100m;
        var bytes = await File.ReadAllBytesAsync(request.OriginalFilePath, cancellationToken);

        foreach (var map in request.MapDefinitions)
        {
            for (var index = 0; index < map.AddressRange.Length; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var absoluteOffset = map.AddressRange.StartOffset + index;
                var current = bytes[(int)absoluteOffset];
                var proposed = decimal.Round(current * multiplier, 0, MidpointRounding.AwayFromZero);

                if (map.MinimumValue is not null && proposed < map.MinimumValue)
                {
                    blockReasons.Add($"Map '{map.MapId}' proposed value is below the minimum limit.");
                }

                if (map.MaximumValue is not null && proposed > map.MaximumValue)
                {
                    blockReasons.Add($"Map '{map.MapId}' proposed value is above the maximum limit.");
                }

                if (proposed is < byte.MinValue or > byte.MaxValue)
                {
                    blockReasons.Add($"Map '{map.MapId}' proposed value is outside byte range.");
                }

                if (blockReasons.Count > 0)
                {
                    continue;
                }

                var proposedByte = (byte)proposed;
                if (proposedByte != current)
                {
                    changes.Add(new CalibrationChange(
                        map.MapId,
                        index,
                        proposedByte,
                        $"Automatic {request.RequestedPercentage:0.######}% intent for profile '{request.ProfileKey}'."));
                }
            }
        }

        if (blockReasons.Count > 0)
        {
            return new PercentageMapChangePlanResult(false, [], [], blockReasons.Distinct().ToArray());
        }

        return new PercentageMapChangePlanResult(
            true,
            changes,
            [$"Prepared {changes.Count} byte-level changes from percentage intent. No file was written."],
            []);
    }

    private static List<string> Validate(PercentageMapChangePlanRequest request)
    {
        var blockReasons = new List<string>();
        if (string.IsNullOrWhiteSpace(request.OriginalFilePath) || !File.Exists(request.OriginalFilePath))
        {
            blockReasons.Add("Original ECU file could not be found.");
        }

        if (string.IsNullOrWhiteSpace(request.ProfileKey))
        {
            blockReasons.Add("Calibration profile key is required.");
        }

        if (request.RequestedPercentage == 0)
        {
            blockReasons.Add("Requested percentage must be different from zero for map change planning.");
        }

        if (request.MapDefinitions.Count == 0)
        {
            blockReasons.Add("At least one verified map definition is required.");
        }

        foreach (var map in request.MapDefinitions)
        {
            if (!IsDummyOrTestMap(map))
            {
                blockReasons.Add($"Map '{map.MapId}' is not from a dummy/test profile. Real ECU map planning remains blocked.");
            }

            if (map.SupportStatus != CalibrationMapDefinitionSupportStatus.Verified)
            {
                blockReasons.Add($"Map '{map.MapId}' is not verified.");
            }

            if (map.IsEmissionsRelated)
            {
                blockReasons.Add($"Map '{map.MapId}' is emissions-related and cannot be changed.");
            }

            if (map.DataType != CalibrationMapDataType.UInt8)
            {
                blockReasons.Add($"Map '{map.MapId}' data type is not supported for percentage planning yet.");
            }

            if (map.Factor != 1m)
            {
                blockReasons.Add($"Map '{map.MapId}' factor must be 1 for byte-level percentage planning.");
            }

            if (map.AddressRange.StartOffset < 0 || map.AddressRange.Length <= 0)
            {
                blockReasons.Add($"Map '{map.MapId}' has an invalid address range.");
            }
            else if (File.Exists(request.OriginalFilePath))
            {
                var fileSize = new FileInfo(request.OriginalFilePath).Length;
                if (map.AddressRange.EndOffset >= fileSize)
                {
                    blockReasons.Add($"Map '{map.MapId}' is outside the original ECU file.");
                }
            }
        }

        return blockReasons.Distinct().ToList();
    }

    private static bool IsDummyOrTestMap(CalibrationMapDefinition map) =>
        map.ProfileId.Contains("dummy", StringComparison.OrdinalIgnoreCase)
        || map.ProfileId.Contains("test", StringComparison.OrdinalIgnoreCase);
}
