using SafeEcu.Application.CalibrationPatching;
using SafeEcu.Application.Persistence;
using SafeEcu.Domain.Projects;

namespace SafeEcu.Application.Projects;

public sealed class EcuProjectMapSnapshotService
{
    private const int MaximumSnapshotCells = 4096;

    private readonly IEcuProjectMapDefinitionRepository _mapDefinitionRepository;

    public EcuProjectMapSnapshotService(IEcuProjectMapDefinitionRepository mapDefinitionRepository)
    {
        _mapDefinitionRepository = mapDefinitionRepository;
    }

    public async Task<EcuProjectMapSnapshotResult> CreateSnapshotAsync(
        EcuProjectMapSnapshotRequest request,
        CancellationToken cancellationToken = default)
    {
        var blockReasons = new List<string>
        {
            "Map snapshot is read-only. It cannot edit ECU data, recommend values or approve calibration changes."
        };
        var errors = new List<string>();

        if (request.ProjectId == Guid.Empty)
        {
            errors.Add("ECU project is required.");
        }

        if (string.IsNullOrWhiteSpace(request.MapId))
        {
            errors.Add("Map id is required.");
        }

        if (string.IsNullOrWhiteSpace(request.FilePath))
        {
            errors.Add("Source ECU file is required.");
        }
        else if (!File.Exists(request.FilePath))
        {
            errors.Add("Source ECU file could not be found.");
        }

        EcuProjectMapDefinition? definition = null;
        if (errors.Count == 0)
        {
            definition = await _mapDefinitionRepository.GetByProjectAndMapIdAsync(
                request.ProjectId,
                request.MapId,
                cancellationToken);
            if (definition is null)
            {
                errors.Add("Map definition was not found in the selected ECU project.");
            }
        }

        if (definition is not null)
        {
            ValidateDefinition(definition, request.FilePath, errors);
        }

        if (errors.Count > 0 || definition is null)
        {
            return new EcuProjectMapSnapshotResult(false, [], 0, 0, "raw", [], errors.Concat(blockReasons).ToArray());
        }

        var dataTypeSize = GetDataTypeSize(definition.DataType);
        var rowCount = definition.RowCount ?? 1;
        var columnCount = definition.ColumnCount ?? definition.Length / dataTypeSize;
        var cells = new List<EcuProjectMapSnapshotCell>(rowCount * columnCount);

        await using var stream = File.OpenRead(request.FilePath);
        for (var row = 0; row < rowCount; row++)
        {
            for (var column = 0; column < columnCount; column++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var cellOffset = definition.StartOffset + ((row * columnCount) + column) * dataTypeSize;
                var rawValue = ReadRawValue(stream, cellOffset, definition.DataType, definition.Endianess);
                var convertedValue = decimal.Round((rawValue * definition.Factor) + definition.OffsetCorrection, 6);
                cells.Add(new EcuProjectMapSnapshotCell(row, column, cellOffset, rawValue, convertedValue));
            }
        }

        var messages = new[]
        {
            $"Rendered map '{definition.MapId}' as a read-only {rowCount}x{columnCount} snapshot.",
            "Raw and converted values are shown for analysis only."
        };

        return new EcuProjectMapSnapshotResult(true, cells, rowCount, columnCount, definition.Unit, messages, blockReasons);
    }

    private static void ValidateDefinition(
        EcuProjectMapDefinition definition,
        string filePath,
        ICollection<string> errors)
    {
        if (definition.IsEmissionsRelated)
        {
            errors.Add("Emissions-related map snapshots are blocked.");
        }

        var dataTypeSize = GetDataTypeSize(definition.DataType);
        if (dataTypeSize <= 0)
        {
            errors.Add("Map data type is not supported for read-only snapshots.");
            return;
        }

        var rowCount = definition.RowCount ?? 1;
        var columnCount = definition.ColumnCount ?? definition.Length / dataTypeSize;
        if (rowCount <= 0 || columnCount <= 0)
        {
            errors.Add("Map dimensions are invalid.");
        }

        if (rowCount * columnCount > MaximumSnapshotCells)
        {
            errors.Add($"Map snapshot is limited to {MaximumSnapshotCells} cells.");
        }

        var requiredLength = rowCount * columnCount * dataTypeSize;
        if (requiredLength > definition.Length)
        {
            errors.Add("Map dimensions exceed the defined address range.");
        }

        var fileInfo = new FileInfo(filePath);
        if (definition.StartOffset < 0 || definition.StartOffset + requiredLength > fileInfo.Length)
        {
            errors.Add("Map address range is outside the selected ECU file.");
        }

        if (definition.Factor <= 0)
        {
            errors.Add("Map conversion factor must be greater than zero.");
        }
    }

    private static int GetDataTypeSize(string dataType) =>
        ParseDataType(dataType) switch
        {
            CalibrationMapDataType.UInt8 or CalibrationMapDataType.Int8 => 1,
            CalibrationMapDataType.UInt16 or CalibrationMapDataType.Int16 => 2,
            CalibrationMapDataType.UInt32 or CalibrationMapDataType.Int32 => 4,
            _ => 0
        };

    private static long ReadRawValue(Stream stream, long offset, string dataType, string endianess)
    {
        var parsedDataType = ParseDataType(dataType);
        var size = GetDataTypeSize(dataType);
        var buffer = new byte[size];
        stream.Position = offset;
        var read = stream.Read(buffer, 0, size);
        if (read != size)
        {
            throw new InvalidOperationException("Could not read map cell from ECU file.");
        }

        if (size > 1 && ShouldReverse(endianess))
        {
            Array.Reverse(buffer);
        }

        return parsedDataType switch
        {
            CalibrationMapDataType.UInt8 => buffer[0],
            CalibrationMapDataType.Int8 => unchecked((sbyte)buffer[0]),
            CalibrationMapDataType.UInt16 => BitConverter.ToUInt16(buffer, 0),
            CalibrationMapDataType.Int16 => BitConverter.ToInt16(buffer, 0),
            CalibrationMapDataType.UInt32 => BitConverter.ToUInt32(buffer, 0),
            CalibrationMapDataType.Int32 => BitConverter.ToInt32(buffer, 0),
            _ => throw new NotSupportedException("Map data type is not supported.")
        };
    }

    private static CalibrationMapDataType ParseDataType(string dataType) =>
        Enum.TryParse<CalibrationMapDataType>(dataType, ignoreCase: true, out var parsed)
            ? parsed
            : CalibrationMapDataType.Unknown;

    private static bool ShouldReverse(string endianess)
    {
        var systemLittleEndian = BitConverter.IsLittleEndian;
        var requestedLittleEndian = !string.Equals(endianess, "BigEndian", StringComparison.OrdinalIgnoreCase);

        return systemLittleEndian != requestedLittleEndian;
    }
}
