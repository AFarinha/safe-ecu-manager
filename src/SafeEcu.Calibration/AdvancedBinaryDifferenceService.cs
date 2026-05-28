namespace SafeEcu.Calibration;

public sealed class AdvancedBinaryDifferenceService
{
    public async Task<BinaryDifferenceScanResult> ScanAsync(
        string originalFilePath,
        string modifiedFilePath,
        BinaryDifferenceScanOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originalFilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(modifiedFilePath);

        if (!File.Exists(originalFilePath))
        {
            throw new FileNotFoundException("Original ECU file could not be found.", originalFilePath);
        }

        if (!File.Exists(modifiedFilePath))
        {
            throw new FileNotFoundException("Modified ECU file could not be found.", modifiedFilePath);
        }

        var scanOptions = options ?? BinaryDifferenceScanOptions.Default;
        if (scanOptions.MaxSampledDifferences < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Maximum sampled differences cannot be negative.");
        }

        if (scanOptions.MaxBlocks < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Maximum blocks cannot be negative.");
        }

        await using var originalStream = File.OpenRead(originalFilePath);
        await using var modifiedStream = File.OpenRead(modifiedFilePath);

        var originalSize = originalStream.Length;
        var modifiedSize = modifiedStream.Length;
        var comparedLength = Math.Max(originalSize, modifiedSize);
        var differences = new List<BinaryDifference>();
        var blocks = new List<BinaryDifferenceBlock>();
        var differenceCount = 0L;
        var currentBlockStart = -1L;
        var currentBlockEnd = -1L;
        var currentBlockDifferences = 0L;
        var isSampleTruncated = false;
        var areBlocksTruncated = false;

        for (var offset = 0L; offset < comparedLength; offset++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var originalByte = ReadByteOrMissing(originalStream, offset, originalSize);
            var modifiedByte = ReadByteOrMissing(modifiedStream, offset, modifiedSize);

            if (originalByte == modifiedByte)
            {
                FinalizeCurrentBlock();
                continue;
            }

            differenceCount++;
            if (differences.Count < scanOptions.MaxSampledDifferences)
            {
                differences.Add(new BinaryDifference(offset, originalByte, modifiedByte));
            }
            else
            {
                isSampleTruncated = true;
            }

            if (currentBlockStart < 0)
            {
                currentBlockStart = offset;
            }

            currentBlockEnd = offset;
            currentBlockDifferences++;
        }

        FinalizeCurrentBlock();

        var percentChanged = comparedLength == 0
            ? 0
            : decimal.Round(differenceCount * 100m / comparedLength, 6);

        return new BinaryDifferenceScanResult(
            originalSize,
            modifiedSize,
            differenceCount,
            percentChanged,
            differences,
            blocks,
            isSampleTruncated,
            areBlocksTruncated);

        void FinalizeCurrentBlock()
        {
            if (currentBlockStart < 0)
            {
                return;
            }

            if (blocks.Count < scanOptions.MaxBlocks)
            {
                blocks.Add(new BinaryDifferenceBlock(currentBlockStart, currentBlockEnd, currentBlockDifferences));
            }
            else
            {
                areBlocksTruncated = true;
            }

            currentBlockStart = -1;
            currentBlockEnd = -1;
            currentBlockDifferences = 0;
        }
    }

    private static byte? ReadByteOrMissing(Stream stream, long offset, long length)
    {
        if (offset >= length)
        {
            return null;
        }

        var value = stream.ReadByte();
        if (value < 0)
        {
            return null;
        }

        return (byte)value;
    }
}
