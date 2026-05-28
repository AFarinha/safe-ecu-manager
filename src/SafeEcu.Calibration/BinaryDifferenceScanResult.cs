using System.Globalization;
using System.Text;

namespace SafeEcu.Calibration;

public sealed record BinaryDifferenceScanResult(
    long OriginalSizeBytes,
    long ModifiedSizeBytes,
    long DifferenceCount,
    decimal PercentChanged,
    IReadOnlyList<BinaryDifference> SampledDifferences,
    IReadOnlyList<BinaryDifferenceBlock> Blocks,
    bool IsSampleTruncated,
    bool AreBlocksTruncated)
{
    public string ToCsv()
    {
        var builder = new StringBuilder();
        builder.AppendLine("Offset,OriginalByte,ModifiedByte");

        foreach (var difference in SampledDifferences)
        {
            builder
                .Append(difference.Offset.ToString(CultureInfo.InvariantCulture))
                .Append(',')
                .Append(FormatByte(difference.OriginalByte))
                .Append(',')
                .Append(FormatByte(difference.ModifiedByte))
                .AppendLine();
        }

        return builder.ToString();
    }

    private static string FormatByte(byte? value) =>
        value is null ? "Missing" : $"0x{value.Value:X2}";
}
