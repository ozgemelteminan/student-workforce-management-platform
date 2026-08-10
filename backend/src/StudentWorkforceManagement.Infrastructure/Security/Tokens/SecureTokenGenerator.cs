using System.Security.Cryptography;
using StudentWorkforceManagement.Application.Common.Security;

namespace StudentWorkforceManagement.Infrastructure.Security.Tokens;

public sealed class SecureTokenGenerator : ISecureTokenGenerator
{
    private const int TokenByteLength = 32;

    public string GenerateToken()
    {
        return Base64UrlEncode(RandomNumberGenerator.GetBytes(TokenByteLength));
    }

    public string HashToken(string rawToken)
    {
        var tokenBytes = System.Text.Encoding.UTF8.GetBytes(rawToken);
        return Convert.ToHexString(SHA256.HashData(tokenBytes));
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
