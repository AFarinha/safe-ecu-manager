namespace SafeEcu.Application.Programmers;

public sealed record ProgrammerCapability(
    string Name,
    string ConnectionType,
    ProgrammerSupportedMode SupportedMode,
    bool DirectReadSupported,
    bool DirectWriteSupported,
    bool RequiresExternalSoftware,
    string Notes);
