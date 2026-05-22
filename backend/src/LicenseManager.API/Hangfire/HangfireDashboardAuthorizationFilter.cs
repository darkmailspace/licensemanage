using System.Security.Claims;
using Hangfire.Dashboard;

namespace LicenseManager.API.Hangfire;

/// <summary>
/// Authorization filter for the Hangfire dashboard.
///
/// * Development: anonymous access is allowed for easier local inspection.
/// * Other environments: requires an authenticated user whose numeric role
///   claim is &lt;= 2 (i.e. SuperAdmin=1 or Admin=2 - matches
///   <see cref="LicenseManager.API.Authorization.Policies.Admin"/>).
///
/// Note: the project encodes roles as numeric strings on the JWT, not as
/// named roles, so <c>IsInRole("Admin")</c> does not work here.
/// </summary>
public sealed class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    private const int AdminMaxRoleLevel = 2;

    private readonly IWebHostEnvironment _environment;

    public HangfireDashboardAuthorizationFilter(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        if (_environment.IsDevelopment())
        {
            return true;
        }

        var user = httpContext.User;
        if (user?.Identity is null || !user.Identity.IsAuthenticated)
        {
            return false;
        }

        var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;
        return int.TryParse(roleClaim, out var roleLevel) && roleLevel <= AdminMaxRoleLevel;
    }
}
