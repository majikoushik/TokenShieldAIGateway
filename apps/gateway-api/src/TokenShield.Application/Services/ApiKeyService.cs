using System.Security.Cryptography;
using System.Text;

namespace TokenShield.Application.Services;

public class ApiKeyService
{
    public (string RawKey, string KeyHash) GenerateKey(string prefix)
    {
        var randomBytes = new byte[24];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        var keyContent = Convert.ToHexString(randomBytes).ToLowerInvariant();
        var rawKey = $"{prefix}{keyContent}";
        var keyHash = HashKey(rawKey);

        return (rawKey, keyHash);
    }

    public string HashKey(string rawKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
