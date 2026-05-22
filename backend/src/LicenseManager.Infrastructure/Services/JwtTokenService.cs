using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LicenseManager.Application.Common.Interfaces;
using LicenseManager.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace LicenseManager.Infrastructure.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _config;
    private readonly ILogger<JwtTokenService> _logger;

    public JwtTokenService(IConfiguration config, ILogger<JwtTokenService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public JwtTokenResult Generate(AdminUser user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, ((int)user.Role).ToString()),
            new("role_name", user.Role.ToString()),
            new("user_type", "admin"),
        };

        return BuildToken(claims, user.Id);
    }

    public JwtTokenResult Generate(Customer customer)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, customer.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, customer.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, customer.Id.ToString()),
            new(ClaimTypes.Name, customer.Name),
            new(ClaimTypes.Email, customer.Email),
            new("user_type", "customer"),
            new("customer_code", customer.CustomerCode),
        };

        return BuildToken(claims, customer.Id);
    }

    private JwtTokenResult BuildToken(List<Claim> claims, Guid userId)
    {
        var secret = _config["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is not configured");
        var issuer = _config["Jwt:Issuer"] ?? "LicenseManagerAPI";
        var audience = _config["Jwt:Audience"] ?? "LicenseManagerClient";
        var expiryMinutes = int.TryParse(_config["Jwt:ExpiryMinutes"], out var m) ? m : 60;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = GenerateRefreshToken();

        return new JwtTokenResult(
            accessToken,
            refreshToken,
            expiryMinutes * 60,
            expires);
    }

    public string? ValidateAndGetSubject(string token)
    {
        try
        {
            var secret = _config["Jwt:Secret"] ?? string.Empty;
            var handler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _config["Jwt:Issuer"] ?? "LicenseManagerAPI",
                ValidateAudience = true,
                ValidAudience = _config["Jwt:Audience"] ?? "LicenseManagerClient",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2),
            };

            var principal = handler.ValidateToken(token, parameters, out _);
            return principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Token validation failed");
            return null;
        }
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    public bool ValidateRefreshToken(string token, Guid userId)
    {
        // In production: lookup token in a refresh_tokens table.
        // For Phase 3 we accept any non-empty token tied to a known user.
        return !string.IsNullOrEmpty(token) && userId != Guid.Empty;
    }
}
