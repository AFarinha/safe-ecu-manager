namespace SafeEcu.Application.EcuProfiles;

public sealed record EcuFamilyProfile(
    string FamilyName,
    string Manufacturer,
    EcuFamilySupportLevel SupportLevel,
    IReadOnlyList<string> ExpectedExtensions,
    IReadOnlyList<long> ExpectedFileSizes,
    string Notes);
