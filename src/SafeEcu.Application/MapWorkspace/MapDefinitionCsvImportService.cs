using System.Globalization;
using SafeEcu.Application.CalibrationPatching;

namespace SafeEcu.Application.MapWorkspace;

public sealed class MapDefinitionCsvImportService
{
    private static readonly string[] RequiredHeaders =
    [
        "MapId",
        "DisplayName",
        "ParameterName",
        "StartOffset",
        "Length",
        "DataType",
        "Endianess",
        "Factor",
        "Unit",
        "MinimumValue",
        "MaximumValue",
        "IsEmissionsRelated",
        "RequiredMapIds",
        "ProfileId",
        "SupportStatus"
    ];

    public async Task<MapDefinitionCsvImportResult> ImportAsync(
        string csvFilePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(csvFilePath))
        {
            return Failure("CSV file path is required.");
        }

        if (!File.Exists(csvFilePath))
        {
            return Failure("CSV map definition file could not be found.");
        }

        var lines = await File.ReadAllLinesAsync(csvFilePath, cancellationToken);
        if (lines.Length < 2)
        {
            return Failure("CSV map definition file must contain headers and at least one map row.");
        }

        var headers = SplitCsvLine(lines[0]);
        var headerIndexes = headers
            .Select((header, index) => new { Header = header.Trim(), Index = index })
            .ToDictionary(item => item.Header, item => item.Index, StringComparer.OrdinalIgnoreCase);

        var missingHeaders = RequiredHeaders
            .Where(header => !headerIndexes.ContainsKey(header))
            .ToArray();
        if (missingHeaders.Length > 0)
        {
            return Failure($"CSV map definition file is missing headers: {string.Join(", ", missingHeaders)}.");
        }

        var maps = new List<CalibrationMapDefinition>();
        var errors = new List<string>();

        for (var lineIndex = 1; lineIndex < lines.Length; lineIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(lines[lineIndex]))
            {
                continue;
            }

            var fields = SplitCsvLine(lines[lineIndex]);
            var lineNumber = lineIndex + 1;
            try
            {
                maps.Add(ParseMap(fields, headerIndexes));
            }
            catch (Exception exception) when (exception is FormatException or ArgumentException or IndexOutOfRangeException)
            {
                errors.Add($"Line {lineNumber}: {exception.Message}");
            }
        }

        if (errors.Count > 0)
        {
            return new MapDefinitionCsvImportResult(false, [], [], errors);
        }

        if (maps.Count == 0)
        {
            return Failure("CSV map definition file did not contain any map rows.");
        }

        return new MapDefinitionCsvImportResult(
            true,
            maps,
            [$"Imported {maps.Count} map definitions from CSV."],
            []);
    }

    private static CalibrationMapDefinition ParseMap(
        IReadOnlyList<string> fields,
        IReadOnlyDictionary<string, int> headerIndexes)
    {
        var mapId = GetRequired(fields, headerIndexes, "MapId");
        var displayName = GetRequired(fields, headerIndexes, "DisplayName");
        var parameterName = GetRequired(fields, headerIndexes, "ParameterName");
        var startOffset = ParseLong(GetRequired(fields, headerIndexes, "StartOffset"), "StartOffset");
        var length = ParseInt(GetRequired(fields, headerIndexes, "Length"), "Length");
        var dataType = ParseEnum<CalibrationMapDataType>(GetRequired(fields, headerIndexes, "DataType"), "DataType");
        var endianess = ParseEnum<CalibrationMapEndianess>(GetRequired(fields, headerIndexes, "Endianess"), "Endianess");
        var factor = ParseDecimal(GetRequired(fields, headerIndexes, "Factor"), "Factor");
        var unit = GetRequired(fields, headerIndexes, "Unit");
        var minimumValue = ParseOptionalDecimal(GetOptional(fields, headerIndexes, "MinimumValue"), "MinimumValue");
        var maximumValue = ParseOptionalDecimal(GetOptional(fields, headerIndexes, "MaximumValue"), "MaximumValue");
        var isEmissionsRelated = ParseBool(GetRequired(fields, headerIndexes, "IsEmissionsRelated"), "IsEmissionsRelated");
        var requiredMapIds = GetOptional(fields, headerIndexes, "RequiredMapIds")
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var profileId = GetRequired(fields, headerIndexes, "ProfileId");
        var supportStatus = ParseEnum<CalibrationMapDefinitionSupportStatus>(
            GetRequired(fields, headerIndexes, "SupportStatus"),
            "SupportStatus");

        return new CalibrationMapDefinition(
            mapId,
            displayName,
            parameterName,
            new CalibrationMapAddressRange(startOffset, length),
            isEmissionsRelated,
            requiredMapIds,
            profileId,
            dataType,
            endianess,
            factor,
            unit,
            minimumValue,
            maximumValue,
            supportStatus);
    }

    private static IReadOnlyList<string> SplitCsvLine(string line)
    {
        var values = new List<string>();
        var current = new List<char>();
        var inQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                if (inQuotes && index + 1 < line.Length && line[index + 1] == '"')
                {
                    current.Add('"');
                    index++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            if (character == ',' && !inQuotes)
            {
                values.Add(new string(current.ToArray()));
                current.Clear();
                continue;
            }

            current.Add(character);
        }

        values.Add(new string(current.ToArray()));
        return values;
    }

    private static string GetRequired(
        IReadOnlyList<string> fields,
        IReadOnlyDictionary<string, int> headerIndexes,
        string header)
    {
        var value = GetOptional(fields, headerIndexes, header);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new FormatException($"{header} is required.");
        }

        return value.Trim();
    }

    private static string GetOptional(
        IReadOnlyList<string> fields,
        IReadOnlyDictionary<string, int> headerIndexes,
        string header)
    {
        var index = headerIndexes[header];
        return index >= fields.Count ? string.Empty : fields[index].Trim();
    }

    private static TEnum ParseEnum<TEnum>(string value, string fieldName)
        where TEnum : struct
    {
        if (!Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed))
        {
            throw new FormatException($"{fieldName} value '{value}' is not supported.");
        }

        return parsed;
    }

    private static long ParseLong(string value, string fieldName)
    {
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return long.Parse(value[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        return long.Parse(value, CultureInfo.InvariantCulture);
    }

    private static int ParseInt(string value, string fieldName) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : throw new FormatException($"{fieldName} value '{value}' is not a valid integer.");

    private static decimal ParseDecimal(string value, string fieldName) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : throw new FormatException($"{fieldName} value '{value}' is not a valid decimal.");

    private static decimal? ParseOptionalDecimal(string value, string fieldName) =>
        string.IsNullOrWhiteSpace(value) ? null : ParseDecimal(value, fieldName);

    private static bool ParseBool(string value, string fieldName) =>
        bool.TryParse(value, out var parsed)
            ? parsed
            : throw new FormatException($"{fieldName} value '{value}' is not a valid boolean.");

    private static MapDefinitionCsvImportResult Failure(string error) =>
        new(false, [], [], [error]);
}
