namespace SafeEcu.Application.Common;

public sealed class AppConfiguration
{
    public string ApplicationName { get; init; } = "Safe ECU Calibration Manager";

    public string DataDirectory { get; init; } = "data";

    public string LogDirectory { get; init; } = "logs";

    public bool DirectEcuWriteEnabled { get; init; }

    public bool DirectProgrammerControlEnabled { get; init; }
}
