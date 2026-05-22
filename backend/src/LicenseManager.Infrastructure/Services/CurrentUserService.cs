using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LicenseManager.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace LicenseManager.Infrastructure.Services;

/// <summary>
/// Reads the current user from HttpContext claims populated by JwtBearer.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;
    private HttpContext? Http => _httpContextAccessor.HttpContext;

    public Guid? UserId
    {
        get
        {
            var sub = Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                   ?? Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? Email => Principal?.FindFirst(ClaimTypes.Email)?.Value
                          ?? Principal?.FindFirst(JwtRegisteredClaimNames.Email)?.Value;

    public string? FullName => Principal?.FindFirst(ClaimTypes.Name)?.Value;

    public int? Role
    {
        get
        {
            var role = Principal?.FindFirst(ClaimTypes.Role)?.Value;
            return int.TryParse(role, out var r) ? r : null;
        }
    }

    public string? RoleName => Principal?.FindFirst("role_name")?.Value;

    public string? IpAddress
    {
        get
        {
            if (Http == null) return null;
            // Honour X-Forwarded-For when behind proxy
            var forwarded = Http.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwarded))
                return forwarded.Split(',')[0].Trim();
            return Http.Connection.RemoteIpAddress?.ToString();
        }
    }

    public string? UserAgent => Http?.Request.Headers.UserAgent.ToString();

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public bool IsInRole(int role) => Role == role;

    /// <summary>
    /// Lower role number = higher privilege (1 = SuperAdmin, 4 = Viewer).
    /// HasMinimumRole(2) means the user is at least an Admin.
    /// </summary>
    public bool HasMinimumRole(int role) => Role.HasValue && Role.Value <= role;
}
