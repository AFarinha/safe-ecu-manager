using SafeEcu.Application.Common;

namespace SafeEcu.Tests;

public sealed class AppConfigurationTests
{
    [Fact]
    public void Dangerous_runtime_capabilities_are_disabled_by_default()
    {
        var configuration = new AppConfiguration();

        Assert.False(configuration.DirectEcuWriteEnabled);
        Assert.False(configuration.DirectProgrammerControlEnabled);
    }
}
