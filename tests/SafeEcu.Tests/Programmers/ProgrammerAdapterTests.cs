using SafeEcu.Application.Programmers;
using SafeEcu.Programmers;
using SafeEcu.Programmers.Adapters;

namespace SafeEcu.Tests.Programmers;

public sealed class ProgrammerAdapterTests
{
    [Fact]
    public void Default_catalog_contains_expected_safe_adapters()
    {
        var capabilities = new ProgrammerCapabilityService(ProgrammerAdapterCatalog.CreateDefaultAdapters())
            .GetCapabilities();

        Assert.Contains(capabilities, capability => capability.SupportedMode == ProgrammerSupportedMode.FileOnly);
        Assert.Contains(capabilities, capability => capability.SupportedMode == ProgrammerSupportedMode.Mock);
        Assert.Contains(capabilities, capability => capability.SupportedMode == ProgrammerSupportedMode.ManualWorkflowOnly);
    }

    [Fact]
    public void Galletto1260_is_manual_workflow_only_without_direct_read_or_write()
    {
        var capability = new Galletto1260Adapter().GetCapability();

        Assert.Equal("Galletto 1260 / EOBD Programmer 1260", capability.Name);
        Assert.Equal("USB/OBD", capability.ConnectionType);
        Assert.Equal(ProgrammerSupportedMode.ManualWorkflowOnly, capability.SupportedMode);
        Assert.False(capability.DirectReadSupported);
        Assert.False(capability.DirectWriteSupported);
        Assert.True(capability.RequiresExternalSoftware);
    }

    [Theory]
    [InlineData(typeof(FileOnlyProgrammerAdapter))]
    [InlineData(typeof(MockProgrammerAdapter))]
    [InlineData(typeof(Galletto1260Adapter))]
    public async Task Adapters_reject_direct_write(Type adapterType)
    {
        var adapter = (IProgrammerAdapter)Activator.CreateInstance(adapterType)!;

        var result = await adapter.WriteAsync();

        Assert.False(result.IsSuccess);
    }
}
