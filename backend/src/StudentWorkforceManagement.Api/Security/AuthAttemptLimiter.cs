using Microsoft.Extensions.Caching.Memory;

namespace StudentWorkforceManagement.Api.Security;

public sealed class AuthAttemptLimiter(IMemoryCache cache)
{
    public AuthLimitResult Check(string purpose, string account, string ipAddress, int maxAttempts, TimeSpan window)
    {
        var key = BuildKey(purpose, account, ipAddress);
        var state = cache.Get<AuthAttemptState>(key);
        if (state is null || state.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return AuthLimitResult.Ok;
        }

        return state.Count >= maxAttempts
            ? new AuthLimitResult(false, state.ExpiresAt - DateTimeOffset.UtcNow)
            : AuthLimitResult.Ok;
    }

    public void RecordFailure(string purpose, string account, string ipAddress, TimeSpan window)
    {
        var key = BuildKey(purpose, account, ipAddress);
        var now = DateTimeOffset.UtcNow;
        var state = cache.Get<AuthAttemptState>(key);
        if (state is null || state.ExpiresAt <= now)
        {
            state = new AuthAttemptState(0, now.Add(window));
        }

        cache.Set(key, state with { Count = state.Count + 1 }, state.ExpiresAt);
    }

    public void Reset(string purpose, string account, string ipAddress)
    {
        cache.Remove(BuildKey(purpose, account, ipAddress));
    }

    private static string BuildKey(string purpose, string account, string ipAddress)
    {
        return $"auth-attempt:{purpose}:{account.Trim().ToLowerInvariant()}:{ipAddress}";
    }

    private sealed record AuthAttemptState(int Count, DateTimeOffset ExpiresAt);
}

public readonly record struct AuthLimitResult(bool Allowed, TimeSpan RetryAfter)
{
    public static AuthLimitResult Ok { get; } = new(true, TimeSpan.Zero);
}
