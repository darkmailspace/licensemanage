namespace LicenseManager.Application.Common.Interfaces;

/// <summary>
/// Provides information about the user making the current request.
/// Backed by HttpContext claims in the API layer.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    string? FullName { get; }
    int? Role { get; }
    string? RoleName { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(int role);
    bool HasMinimumRole(int role);
}
