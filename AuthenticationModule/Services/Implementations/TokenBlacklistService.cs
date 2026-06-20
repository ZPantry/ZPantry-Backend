using AuthenticationModule.Services.Interfaces;
using System.Collections.Concurrent;

namespace AuthenticationModule.Services.Implementations;

public class TokenBlacklistService : ITokenBlacklistService
{
    private static readonly ConcurrentDictionary<string, DateTimeOffset> RevokedTokens = new();

    public Task RevokeAsync(string jti, DateTimeOffset expiresAt)
    {
        RevokedTokens[jti] = expiresAt;
        return Task.CompletedTask;
    }

    public Task<bool> IsRevokedAsync(string jti)
    {
        if (RevokedTokens.TryGetValue(jti, out var expiresAt))
        {
            if (expiresAt > DateTimeOffset.UtcNow)
            {
                return Task.FromResult(true);
            }

            RevokedTokens.TryRemove(jti, out _);
        }

        return Task.FromResult(false);
    }
}
