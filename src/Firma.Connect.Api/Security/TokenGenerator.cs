using System.Security.Cryptography;
using System.Text;

namespace Firma.Connect.Api.Security;

public static class TokenGenerator
{
    public static string CreateSecureToken()
        => Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

    public static string Hash(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
}
