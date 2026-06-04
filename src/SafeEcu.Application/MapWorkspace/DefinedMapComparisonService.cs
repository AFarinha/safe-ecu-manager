using SafeEcu.Application.CalibrationPatching;

namespace SafeEcu.Application.MapWorkspace;

public sealed class DefinedMapComparisonService
{
    private const int MaximumCells = 512;

    public async Task<DefinedMapComparisonResult> CompareAsync(
        DefinedMapComparisonRequest request,
        CancellationToken cancellationToken = default)
    {
        var blockReasons = Validate(request);
        if (blockReasons.Count > 0)
        {
            return new DefinedMapComparisonResult(false, request.MapDefinition.MapId, [], 0, 0, [], blockReasons);
        }

        var bytesPerCell = GetByteWidth(request.MapDefinition.DataType);
        var cellCount = request.MapDefinition.AddressRange.Length / bytesPerCell;
        var points = new List<DefinedMapComparisonPoint>(cellCount);

        await using var originalStream = File.OpenRead(request.OriginalFilePath);
        await using var modifiedStream = File.OpenRead(request.ModifiedFilePath);

        for (var cellIndex = 0; cellIndex < cellCount; cellIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var offset = request.MapDefinition.AddressRange.StartOffset + cellIndex * bytesPerCell;
            var original = ReadScaledValue(originalStream, offset, request.MapDefinition);
            var modified = ReadScaledValue(modifiedStream, offset, request.MapDefinition);
            var delta = modified - original;
            var percentDelta = original == 0 ? 0 : decimal.Round(delta * 100m / original, 6);
            points.Add(new DefinedMapComparisonPoint(cellIndex, offset, original, modified, delta, percentDelta));
        }

        var changedCells = points.LongCount(point => point.Delta != 0);
        var averageDelta = points.Count == 0
            ? 0
            : decimal.Round(points.Sum(point => Math.Abs(point.Delta)) / points.Count, 6);

        return new DefinedMapComparisonResult(
            true,
            request.MapDefinition.MapId,
            points,
            changedCells,
            averageDelta,
            [$"Compared {points.Count} cells for map '{request.MapDefinition.DisplayName}'."],
            ["Defined map comparison is read-only and does not generate modified files."]);
    }

    private static List<string> Validate(DefinedMapComparisonRequest request)
    {
        var blockReasons = new List<string>();
        if (!File.Exists(request.OriginalFilePath))
        {
            blockReasons.Add("Original ECU file could not be found.");
        }

        if (!File.Exists(request.ModifiedFilePath))
        {
            blockReasons.Add("Modified ECU file could not be found.");
        }

        if (request.MapDefinition.SupportStatus != CalibrationMapDefinitionSupportStatus.Verified)
        {
            blockReasons.Add($"Map '{request.MapDefinition.MapId}' is not verified.");
        }

        if (request.MapDefinition.IsEmissionsRelated)
        {
            blockReasons.Add($"Map '{request.MapDefinition.MapId}' is emissions-related and cannot be used.");
        }

        if (request.MapDefinition.AddressRange.StartOffset < 0 || request.MapDefinition.AddressRange.Length <= 0)
        {
            blockReasons.Add($"Map '{request.MapDefinition.MapId}' has an invalid address range.");
        }

        var bytesPerCell = GetByteWidth(request.MapDefinition.DataType);
        if (bytesPerCell <= 0)
        {
            blockReasons.Add($"Map '{request.MapDefinition.MapId}' has an unsupported data type.");
        }
        else if (request.MapDefinition.AddressRange.Length % bytesPerCell != 0)
        {
            blockReasons.Add($"Map '{request.MapDefinition.MapId}' length is not aligned to its data type.");
        }
        else if (request.MapDefinition.AddressRange.Length / bytesPerCell > MaximumCells)
        {
            blockReasons.Add($"Map '{request.MapDefinition.MapId}' is limited to {MaximumCells} cells for safe preview.");
        }

        if (request.MapDefinition.Factor <= 0)
        {
            blockReasons.Add($"Map '{request.MapDefinition.MapId}' must define a positive conversion factor.");
        }

        if (File.Exists(request.OriginalFilePath))
        {
            var originalSize = new FileInfo(request.OriginalFilePath).Length;
            if (request.MapDefinition.AddressRange.EndOffset >= originalSize)
            {
                blockReasons.Add($"Map '{request.MapDefinition.MapId}' is outside the original ECU file.");
            }
        }

        if (File.Exists(request.ModifiedFilePath))
        {
            var modifiedSize = new FileInfo(request.ModifiedFilePath).Length;
            if (request.MapDefinition.AddressRange.EndOffset >= modifiedSize)
            {
                blockReasons.Add($"Map '{request.MapDefinition.MapId}' is outside the modified ECU file.");
            }
        }

        return blockReasons;
    }

    private static decimal ReadScaledValue(
        Stream stream,
        long offset,
        CalibrationMapDefinition definition)
    {
        var bytesPerCell = GetByteWidth(definition.DataType);
        var buffer = new byte[bytesPerCell];
        stream.Position = offset;
        var read = stream.Read(buffer, 0, buffer.Length);
        if (read != buffer.Length)
        {
            throw new EndOfStreamException("Map value could not be read.");
        }

        if (definition.Endianess == CalibrationMapEndianess.BigEndian)
        {
            Array.Reverse(buffer);
        }

        var raw = definition.DataType switch
        {
            CalibrationMapDataType.UInt8 => (decimal)buffer[0],
            CalibrationMapDataType.Int8 => unchecked((decimal)(sbyte)buffer[0]),
            CalibrationMapDataType.UInt16 => BitConverter.ToUInt16(buffer),
            CalibrationMapDataType.Int16 => BitConverter.ToInt16(buffer),
            CalibrationMapDataType.UInt32 => BitConverter.ToUInt32(buffer),
            CalibrationMapDataType.Int32 => BitConverter.ToInt32(buffer),
            _ => throw new NotSupportedException("Unsupported map data type.")
        };

        return decimal.Round(raw * definition.Factor, 6);
    }

    private static int GetByteWidth(CalibrationMapDataType dataType) =>
        dataType switch
        {
            CalibrationMapDataType.UInt8 or CalibrationMapDataType.Int8 => 1,
            CalibrationMapDataType.UInt16 or CalibrationMapDataType.Int16 => 2,
            CalibrationMapDataType.UInt32 or CalibrationMapDataType.Int32 => 4,
            _ => 0
        };
}
