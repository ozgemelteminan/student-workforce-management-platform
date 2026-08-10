using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using StudentWorkforceManagement.Application.Auth.Services;
using StudentWorkforceManagement.Domain.Entities;

namespace StudentWorkforceManagement.Infrastructure.Security.Tokens;

public sealed class JwtAccessTokenService(IOptions<JwtOptions> options) : IAccessTokenService
{
    private readonly JwtOptions _options = options.Value;

    public string CreateAccessToken(User user, IReadOnlyCollection<string> roles, Guid sessionId)
    {
        var now = DateTimeOffset.UtcNow;
        var claims = new Dictionary<string, object>
        {
            [ClaimTypes.NameIdentifier] = user.Id.ToString("D"),
            ["sub"] = user.Id.ToString("D"),
            ["sid"] = sessionId.ToString("D"),
            ["jti"] = Guid.NewGuid().ToString("N"),
            ["roles"] = roles.ToArray()
        };

        var header = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, object>
        {
            ["alg"] = "HS256",
            ["typ"] = "JWT"
        }));
        var payload = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, object>
        {
            ["iss"] = _options.Issuer,
            ["aud"] = _options.Audience,
            ["iat"] = now.ToUnixTimeSeconds(),
            ["nbf"] = now.ToUnixTimeSeconds(),
            ["exp"] = now.AddMinutes(_options.AccessTokenMinutes).ToUnixTimeSeconds(),
            ["claims"] = claims
        }));
        var unsignedToken = $"{header}.{payload}";
        var signature = Sign(unsignedToken, _options.SigningKey);
        return $"{unsignedToken}.{signature}";
    }

    private static string Sign(string value, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        return Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(value)));
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
