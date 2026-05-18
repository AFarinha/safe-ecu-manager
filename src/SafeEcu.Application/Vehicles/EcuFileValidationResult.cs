namespace SafeEcu.Application.Vehicles;

public sealed class EcuFileValidationResult
{
    private readonly List<string> _messages = [];
    private readonly List<string> _warnings = [];
    private readonly List<string> _errors = [];

    public bool IsValid => _errors.Count == 0;

    public EcuFileValidationSeverity Severity
    {
        get
        {
            if (_errors.Count > 0)
            {
                return EcuFileValidationSeverity.Error;
            }

            return _warnings.Count > 0 ? EcuFileValidationSeverity.Warning : EcuFileValidationSeverity.Info;
        }
    }

    public IReadOnlyList<string> Messages => _messages;

    public IReadOnlyList<string> Warnings => _warnings;

    public IReadOnlyList<string> Errors => _errors;

    public void AddMessage(string message) => _messages.Add(message);

    public void AddWarning(string warning) => _warnings.Add(warning);

    public void AddError(string error) => _errors.Add(error);

    public string ToDisplayText()
    {
        var lines = _errors
            .Concat(_warnings)
            .Concat(_messages)
            .ToArray();

        return lines.Length == 0 ? "Validation completed." : string.Join(Environment.NewLine, lines);
    }
}
