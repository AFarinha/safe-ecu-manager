namespace SafeEcu.Application.ControlledParameters;

public sealed class ControlledParameterRuleEngine
{
    private readonly ControlledParameterRuleCatalog _catalog;

    public ControlledParameterRuleEngine(ControlledParameterRuleCatalog catalog)
    {
        _catalog = catalog;
    }

    public ControlledParameterValidationResult Validate(ControlledParameterProposal proposal)
    {
        if (string.IsNullOrWhiteSpace(proposal.ProfileKey))
        {
            return Block("Calibration profile is required.");
        }

        if (proposal.ParameterNames.Count == 0)
        {
            return Block("At least one controlled parameter is required.");
        }

        var rules = _catalog.ListForProfile(proposal.ProfileKey);
        if (rules.Count == 0)
        {
            return Block($"No controlled parameter rules are defined for profile '{proposal.ProfileKey}'.");
        }

        var allowedNames = rules
            .Select(rule => Normalize(rule.ParameterName))
            .Concat(rules.SelectMany(rule => rule.RequiredParameters).Select(Normalize))
            .ToHashSet();
        var proposedNames = proposal.ParameterNames.Select(Normalize).ToHashSet();
        var blockReasons = new List<string>();
        var messages = new List<string>();

        foreach (var proposedName in proposedNames)
        {
            if (!allowedNames.Contains(proposedName))
            {
                blockReasons.Add($"Parameter '{proposedName}' is not allowed for profile '{proposal.ProfileKey}'.");
            }
        }

        foreach (var rule in rules.Where(rule => proposedNames.Contains(Normalize(rule.ParameterName))))
        {
            var missing = rule.RequiredParameters
                .Where(required => !proposedNames.Contains(Normalize(required)))
                .ToList();

            if (missing.Count > 0)
            {
                blockReasons.Add(
                    $"Parameter '{rule.ParameterName}' is missing required dependencies: {string.Join(", ", missing)}. {rule.Reason}");
            }
            else
            {
                messages.Add($"Parameter '{rule.ParameterName}' has its required dependencies present.");
            }
        }

        if (blockReasons.Count > 0)
        {
            return new ControlledParameterValidationResult(false, ControlledParameterStatus.Blocked, messages, blockReasons);
        }

        return new ControlledParameterValidationResult(true, ControlledParameterStatus.Allowed, messages, []);
    }

    private static ControlledParameterValidationResult Block(string reason) =>
        new(false, ControlledParameterStatus.Blocked, [], [reason]);

    private static string Normalize(string value) =>
        value.Trim();
}
