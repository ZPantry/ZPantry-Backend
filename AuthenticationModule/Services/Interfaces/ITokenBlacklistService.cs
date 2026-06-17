namespace AuthenticationModule.Services.Interfaces;

public interface ITokenBlacklistService
{
    Task RevokeAsync(string jti, DateTimeOffset expiresAt);

    Task<bool> IsRevokedAsync(string jti);
}
