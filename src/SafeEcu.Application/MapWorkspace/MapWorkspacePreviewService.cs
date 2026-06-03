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
}
