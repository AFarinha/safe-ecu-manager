namespace SafeEcu.Application.Projects;

public sealed class EcuProjectMapEditPreviewService
{
    private const int MaximumPreviewCells = 1024;

    public EcuProjectMapEditPreviewResult Preview(EcuProjectMapEditPreviewRequest request)
    {
        var blockReasons = new List<string>
        {
            "Edit preview is in-memory only. It does not write ECU files or approve calibration output."
        };
        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return new EcuProjectMapEditPreviewResult(false, [], [], errors.Concat(blockReasons).ToArray());
        }

        var selectedCells = request.Snapshot.Cells
            .Where(cell =>
                cell.Row >= request.StartRow &&
                cell.Row < request.StartRow + request.RowCount &&
                cell.Column >= request.StartColumn &&
                cell.Column < request.StartColumn + request.ColumnCount)
            .OrderBy(cell => cell.Row)
            .ThenBy(cell => cell.Column)
            .ToArray();

        if (selectedCells.Length == 0)
        {
            return new EcuProjectMapEditPreviewResult(
                false,
                [],
                [],
                errors.Concat(["Selected range does not contain any map cells."]).Concat(blockReasons).ToArray());
        }

        var previewCells = new List<EcuProjectMapEditPreviewCell>();
        foreach (var cell in selectedCells)
        {
            var proposedValue = CalculateProposedValue(cell.ConvertedValue, request.Operation, request.Value);
            if (request.MinimumAllowed is not null && proposedValue < request.MinimumAllowed)
            {
                errors.Add($"Cell {cell.Row},{cell.Column} would go below the allowed minimum.");
            }

            if (request.MaximumAllowed is not null && proposedValue > request.MaximumAllowed)
            {
                errors.Add($"Cell {cell.Row},{cell.Column} would exceed the allowed maximum.");
            }

            previewCells.Add(new EcuProjectMapEditPreviewCell(
                cell.Row,
                cell.Column,
                cell.Offset,
                cell.ConvertedValue,
                decimal.Round(proposedValue, 6),
                decimal.Round(proposedValue - cell.ConvertedValue, 6)));
        }

        if (errors.Count > 0)
        {
            return new EcuProjectMapEditPreviewResult(false, previewCells, [], errors.Concat(blockReasons).ToArray());
        }

        var messages = new[]
        {
            $"Prepared read-only edit preview for {previewCells.Count} map cell(s).",
            "No file bytes were modified."
        };

        return new EcuProjectMapEditPreviewResult(true, previewCells, messages, blockReasons);
    }

    private static List<string> Validate(EcuProjectMapEditPreviewRequest request)
    {
        var errors = new List<string>();
        if (!request.Snapshot.IsSuccess)
        {
            errors.Add("A valid read-only map snapshot is required before edit preview.");
        }

        if (request.StartRow < 0 || request.StartColumn < 0)
        {
            errors.Add("Selected range start cannot be negative.");
        }

        if (request.RowCount <= 0 || request.ColumnCount <= 0)
        {
            errors.Add("Selected range dimensions must be greater than zero.");
        }

        if (request.RowCount * request.ColumnCount > MaximumPreviewCells)
        {
            errors.Add($"Edit preview is limited to {MaximumPreviewCells} cells.");
        }

        if (request.MinimumAllowed is not null && request.MaximumAllowed is not null && request.MinimumAllowed > request.MaximumAllowed)
        {
            errors.Add("Allowed min/max limits are invalid.");
        }

        if (request.Operation == EcuProjectMapEditOperation.ApplyMultiplier && request.Value <= 0)
        {
            errors.Add("Multiplier must be greater than zero.");
        }

        return errors;
    }

    private static decimal CalculateProposedValue(
        decimal currentValue,
        EcuProjectMapEditOperation operation,
        decimal value) =>
        operation switch
        {
            EcuProjectMapEditOperation.SetAbsolute => value,
            EcuProjectMapEditOperation.IncrementAbsolute => currentValue + value,
            EcuProjectMapEditOperation.DecrementAbsolute => currentValue - value,
            EcuProjectMapEditOperation.ApplyPercentage => currentValue * (1 + (value / 100m)),
            EcuProjectMapEditOperation.ApplyMultiplier => currentValue * value,
            _ => currentValue
        };
}
