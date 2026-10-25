using System.Security.Cryptography;
using System.Text;

namespace backend.Data;

public static class IpHasher
{
    public const string SaltKey = "IP_HASH_SALT";

    // Development-only fallback. Not a secret. Production must set IP_HASH_SALT.
    private const string DevOnlyFallbackSalt = "dev-only-ip-hash-salt";

    public static string ResolveSalt(IConfiguration configuration, IHostEnvironment environment, ILogger logger)
    {
        var salt = configuration[SaltKey];
        if (!string.IsNullOrWhiteSpace(salt))
        {
            return salt;
        }

        if (environment.IsDevelopment())
        {
            logger.LogWarning(
                "IP_HASH_SALT is not set. Visitor IPs are hashed with the development-only fallback salt. Set IP_HASH_SALT before running outside Development.");
            return DevOnlyFallbackSalt;
        }

        throw new InvalidOperationException(
            "IP_HASH_SALT is not set. Set the IP_HASH_SALT environment variable to a secret salt used as the HMAC-SHA256 key for visitor IP hashes.");
    }

    public static string? Hash(string? ipAddress, string salt)
    {
        if (string.IsNullOrEmpty(ipAddress))
        {
            return null;
        }

        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(salt), Encoding.UTF8.GetBytes(ipAddress));
        return Convert.ToHexStringLower(hash);
    }
}
