namespace SafeEcu.Application.Programmers;

public sealed class ProgrammerCapabilityService
{
    private readonly IReadOnlyList<IProgrammerAdapter> _adapters;

    public ProgrammerCapabilityService(IEnumerable<IProgrammerAdapter> adapters)
    {
        _adapters = adapters.ToArray();
    }

    public IReadOnlyList<ProgrammerCapability> GetCapabilities() =>
        _adapters.Select(adapter => adapter.GetCapability()).ToArray();
}
