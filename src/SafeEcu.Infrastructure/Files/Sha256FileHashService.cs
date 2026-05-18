using System.Security.Cryptography;
using SafeEcu.Application.Files;

namespace SafeEcu.Infrastructure.Files;

public sealed class Sha256FileHashService : IFileHashService
{
    public async Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(filePath);
        var hashBytes = await SHA256.HashDataAsync(stream, cancellationToken);

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
