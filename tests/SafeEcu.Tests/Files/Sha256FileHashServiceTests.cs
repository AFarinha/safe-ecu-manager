using SafeEcu.Infrastructure.Files;

namespace SafeEcu.Tests.Files;

public sealed class Sha256FileHashServiceTests : IDisposable
{
    private readonly string _testDirectory;

    public Sha256FileHashServiceTests()
    {
        _testDirectory = Path.Combine(AppContext.BaseDirectory, "hash-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public async Task Computes_sha256_for_file()
    {
        var filePath = Path.Combine(_testDirectory, "sample.bin");
        await File.WriteAllBytesAsync(filePath, [0x01, 0x02, 0x03]);

        var hash = await new Sha256FileHashService().ComputeSha256Async(filePath);

        Assert.Equal("039058c6f2c0cb492c533b0a4d14ef77cc0f78abccced5287d84a1a2011cfb81", hash);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }
}
