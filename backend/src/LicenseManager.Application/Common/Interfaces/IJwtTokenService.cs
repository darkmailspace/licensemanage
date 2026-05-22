using LicenseManager.Domain.Entities;

namespace LicenseManager.Application.Common.Interfaces;

public record JwtTokenResult(string AccessToken, string RefreshToken, int ExpiresInSeconds, DateTime ExpiresAt);

public interface IJwtTokenService
{
    JwtTokenResult Generate(AdminUser user);
    JwtTokenResult Generate(Customer customer);
    string? ValidateAndGetSubject(string token);
    string GenerateRefreshToken();
    bool ValidateRefreshToken(string token, Guid userId);
}
