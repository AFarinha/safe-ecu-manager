using SafeEcu.Application.Programmers;
using SafeEcu.Programmers.Adapters;

namespace SafeEcu.Programmers;

public static class ProgrammerAdapterCatalog
{
    public static IReadOnlyList<IProgrammerAdapter> CreateDefaultAdapters() =>
    [
        new FileOnlyProgrammerAdapter(),
        new MockProgrammerAdapter(),
        new Galletto1260Adapter()
    ];
}
