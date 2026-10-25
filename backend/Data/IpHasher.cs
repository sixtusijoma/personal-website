using System.Security.Cryptography;
using System.Text;

namespace backend.Data;

public static class IpHasher
{
    public static string? Hash(string? ip, string salt) =>
        string.IsNullOrEmpty(ip)
            ? null
            : Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(salt), Encoding.UTF8.GetBytes(ip)));
}
