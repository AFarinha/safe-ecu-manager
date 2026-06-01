namespace SafeEcu.Application.CalibrationPatching;

public sealed class CalibrationMapLocator
{
    public async Task<CalibrationMapValue?> LocateAsync(
        string filePath,
        CalibrationMapDefinition definition,
        CalibrationChange change,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        if (definition.AddressRange.StartOffset < 0 || definition.AddressRange.Length <= 0)
        {
            return null;
        }

        var absoluteOffset = definition.AddressRange.StartOffset + change.RelativeOffset;
        if (change.RelativeOffset < 0 || absoluteOffset > definition.AddressRange.EndOffset)
        {
            return null;
        }

        await using var stream = File.OpenRead(filePath);
        if (absoluteOffset >= stream.Length)
        {
            return null;
        }

        stream.Position = absoluteOffset;
        var value = stream.ReadByte();
        if (value < 0)
        {
            return null;
        }

        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();

        return new CalibrationMapValue(
            definition.MapId,
            absoluteOffset,
            (byte)value,
            change.ProposedValue);
    }
}
