namespace SafeEcu.Application.MapWorkspace;

public sealed class MapWorkspacePreviewService
{
    private const int MaximumPreviewLength = 512;

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
}
