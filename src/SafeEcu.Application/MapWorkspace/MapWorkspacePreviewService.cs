namespace SafeEcu.Application.MapWorkspace;

public sealed class MapWorkspacePreviewService
{
    private const int MaximumPreviewLength = 512;
    private const int MaximumSearchLength = 65536;
    private const int DefaultHexBytesPerRow = 16;

    public async Task<MapWorkspacePreviewResult> PreviewRawBytesAsync(
        MapWorkspacePreviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var messages = new List<string>
        {
            "Read-only raw byte visualization. This does not identify, approve or edit ECU maps."
        };
        var blockReasons = new List<string>
        {
            "Map editing is blocked until ECU/software profile, map definition, checksum and safety limits are verified."
        };

        if (string.IsNullOrWhiteSpace(request.FilePath))
        {
            errors.Add("A source ECU file must be selected.");
        }
        else if (!File.Exists(request.FilePath))
        {
            errors.Add("The selected ECU file could not be found.");
        }

        if (request.StartOffset < 0)
        {
            errors.Add("Start offset cannot be negative.");
        }

        if (request.Length <= 0)
        {
            errors.Add("Preview length must be greater than zero.");
        }

        if (request.Length > MaximumPreviewLength)
        {
            errors.Add($"Preview length is limited to {MaximumPreviewLength} bytes.");
        }

        if (request.Factor <= 0)
        {
            errors.Add("Scale factor must be greater than zero.");
        }

        if (errors.Count > 0)
        {
            return new MapWorkspacePreviewResult(false, [], request.Unit, false, messages, errors.Concat(blockReasons).ToArray());
        }

        var fileInfo = new FileInfo(request.FilePath);
        if (request.StartOffset >= fileInfo.Length)
        {
            errors.Add("Start offset is outside the selected ECU file.");
            return new MapWorkspacePreviewResult(false, [], request.Unit, false, messages, errors.Concat(blockReasons).ToArray());
        }

        var readableLength = (int)Math.Min(request.Length, fileInfo.Length - request.StartOffset);
        var buffer = new byte[readableLength];
        await using var stream = File.OpenRead(request.FilePath);
        stream.Position = request.StartOffset;
        var bytesRead = await stream.ReadAsync(buffer, cancellationToken);

        var points = buffer
            .Take(bytesRead)
            .Select((value, index) => new MapWorkspacePreviewPoint(
                request.StartOffset + index,
                value,
                decimal.Round(value * request.Factor, 6)))
            .ToArray();

        return new MapWorkspacePreviewResult(true, points, request.Unit, false, messages, blockReasons);
    }

    public async Task<MapWorkspaceComparisonResult> CompareRawBytesAsync(
        MapWorkspaceComparisonRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = ValidateCommon(request.OriginalFilePath, request.StartOffset, request.Length, "Original ECU file");
        errors.AddRange(ValidateCommon(request.ModifiedFilePath, request.StartOffset, request.Length, "Modified ECU file"));
        var blockReasons = new List<string>
        {
            "Comparison is read-only. It does not identify maps, approve calibration changes or generate modified files."
        };

        if (errors.Count > 0)
        {
            return new MapWorkspaceComparisonResult(false, [], 0, 0, [], errors.Concat(blockReasons).ToArray());
        }

        var points = new List<MapWorkspaceComparisonPoint>();
        await using var originalStream = File.OpenRead(request.OriginalFilePath);
        await using var modifiedStream = File.OpenRead(request.ModifiedFilePath);
        var comparedLength = Math.Min(
            request.Length,
            (int)Math.Max(
                Math.Max(0, originalStream.Length - request.StartOffset),
                Math.Max(0, modifiedStream.Length - request.StartOffset)));

        for (var index = 0; index < comparedLength; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var offset = request.StartOffset + index;
            var original = ReadByteOrMissing(originalStream, offset);
            var modified = ReadByteOrMissing(modifiedStream, offset);
            points.Add(new MapWorkspaceComparisonPoint(
                offset,
                original,
                modified,
                original is null || modified is null ? null : modified.Value - original.Value));
        }

        var differenceCount = points.Count(point => point.OriginalByte != point.ModifiedByte);
        var percentChanged = points.Count == 0 ? 0 : decimal.Round(differenceCount * 100m / points.Count, 6);
        var messages = new[]
        {
            $"Compared {points.Count} bytes in the selected range.",
            $"{differenceCount} byte differences found ({percentChanged:0.######}%)."
        };

        return new MapWorkspaceComparisonResult(true, points, differenceCount, percentChanged, messages, blockReasons);
    }

    public async Task<MapWorkspaceHexViewResult> PreviewHexAsync(
        MapWorkspaceHexViewRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = ValidateCommon(request.FilePath, request.StartOffset, request.Length, "ECU file");
        var blockReasons = new List<string>
        {
            "Hex view is read-only. It cannot edit ECU data or approve calibration changes."
        };

        if (request.BytesPerRow <= 0)
        {
            errors.Add("Bytes per row must be greater than zero.");
        }

        if (request.BytesPerRow > 32)
        {
            errors.Add("Bytes per row is limited to 32.");
        }

        if (errors.Count > 0)
        {
            return new MapWorkspaceHexViewResult(false, [], [], errors.Concat(blockReasons).ToArray());
        }

        var bytesPerRow = request.BytesPerRow == 0 ? DefaultHexBytesPerRow : request.BytesPerRow;
        var bytes = await ReadWindowAsync(request.FilePath, request.StartOffset, request.Length, cancellationToken);
        var rows = bytes
            .Chunk(bytesPerRow)
            .Select((chunk, rowIndex) => new MapWorkspaceHexRow(
                request.StartOffset + (rowIndex * bytesPerRow),
                chunk.Select(value => value.ToString("X2")).ToArray(),
                new string(chunk.Select(ToPrintableAscii).ToArray())))
            .ToArray();
        var messages = new[] { $"Rendered {bytes.Length} bytes as read-only hex rows." };

        return new MapWorkspaceHexViewResult(true, rows, messages, blockReasons);
    }

    public async Task<MapWorkspaceBytePatternSearchResult> SearchBytePatternAsync(
        MapWorkspaceBytePatternSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = ValidateSearch(request.FilePath, request.StartOffset, request.Length);
        var blockReasons = new List<string>
        {
            "Pattern search is read-only. Candidate matches must be manually verified before any map definition is trusted."
        };

        if (request.Length > MaximumSearchLength)
        {
            errors.Add($"Pattern search length is limited to {MaximumSearchLength} bytes.");
        }

        var pattern = ParseHexPattern(request.HexPattern, errors);
        if (errors.Count > 0)
        {
            return new MapWorkspaceBytePatternSearchResult(false, [], [], errors.Concat(blockReasons).ToArray());
        }

        var bytes = await ReadWindowAsync(request.FilePath, request.StartOffset, request.Length, cancellationToken);
        var matches = new List<MapWorkspaceBytePatternMatch>();
        for (var index = 0; index <= bytes.Length - pattern.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (PatternMatches(bytes, index, pattern))
            {
                matches.Add(new MapWorkspaceBytePatternMatch(request.StartOffset + index));
            }
        }

        var messages = new[]
        {
            $"Searched {bytes.Length} bytes for {pattern.Length} byte pattern.",
            $"{matches.Count} match(es) found."
        };

        return new MapWorkspaceBytePatternSearchResult(true, matches, messages, blockReasons);
    }

    private static List<string> ValidateCommon(string filePath, long startOffset, int length, string fileLabel)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(filePath))
        {
            errors.Add($"{fileLabel} must be selected.");
        }
        else if (!File.Exists(filePath))
        {
            errors.Add($"{fileLabel} could not be found.");
        }

        if (startOffset < 0)
        {
            errors.Add("Start offset cannot be negative.");
        }

        if (length <= 0)
        {
            errors.Add("Preview length must be greater than zero.");
        }

        if (length > MaximumPreviewLength)
        {
            errors.Add($"Preview length is limited to {MaximumPreviewLength} bytes.");
        }

        return errors;
    }

    private static List<string> ValidateSearch(string filePath, long startOffset, int length)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(filePath))
        {
            errors.Add("ECU file must be selected.");
        }
        else if (!File.Exists(filePath))
        {
            errors.Add("ECU file could not be found.");
        }

        if (startOffset < 0)
        {
            errors.Add("Start offset cannot be negative.");
        }

        if (length <= 0)
        {
            errors.Add("Pattern search length must be greater than zero.");
        }

        return errors;
    }

    private static byte? ReadByteOrMissing(Stream stream, long offset)
    {
        if (offset >= stream.Length)
        {
            return null;
        }

        stream.Position = offset;
        var value = stream.ReadByte();
        return value < 0 ? null : (byte)value;
    }

    private static async Task<byte[]> ReadWindowAsync(
        string filePath,
        long startOffset,
        int length,
        CancellationToken cancellationToken)
    {
        var fileInfo = new FileInfo(filePath);
        if (startOffset >= fileInfo.Length)
        {
            return [];
        }

        var readableLength = (int)Math.Min(length, fileInfo.Length - startOffset);
        var buffer = new byte[readableLength];
        await using var stream = File.OpenRead(filePath);
        stream.Position = startOffset;
        var bytesRead = await stream.ReadAsync(buffer, cancellationToken);

        return bytesRead == buffer.Length ? buffer : buffer.Take(bytesRead).ToArray();
    }

    private static char ToPrintableAscii(byte value) =>
        value is >= 32 and <= 126 ? (char)value : '.';

    private static byte[] ParseHexPattern(string hexPattern, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(hexPattern))
        {
            errors.Add("A hexadecimal byte pattern is required.");
            return [];
        }

        var tokens = hexPattern
            .Split([' ', ',', ';', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(token => token.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? token[2..] : token)
            .ToArray();

        if (tokens.Length == 0)
        {
            errors.Add("A hexadecimal byte pattern is required.");
            return [];
        }

        var pattern = new List<byte>();
        foreach (var token in tokens)
        {
            if (token.Length != 2 || !byte.TryParse(token, System.Globalization.NumberStyles.HexNumber, null, out var value))
            {
                errors.Add("Hexadecimal byte patterns must use two digit bytes, for example: 01 0A FF.");
                return [];
            }

            pattern.Add(value);
        }

        return pattern.ToArray();
    }

    private static bool PatternMatches(byte[] bytes, int startIndex, byte[] pattern)
    {
        for (var index = 0; index < pattern.Length; index++)
        {
            if (bytes[startIndex + index] != pattern[index])
            {
                return false;
            }
        }

        return true;
    }
}
