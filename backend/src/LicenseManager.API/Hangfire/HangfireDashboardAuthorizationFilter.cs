using Hangfire.Dashboard;

namespace LicenseManager.API.Hangfire;

/// <summary>
/// Authorization filter for the Hangfire dashboard.
/// In Development: anonymous access is allowed for convenience.
/// In other environments: requires an authenticated user with the SuperAdmin or Admin role.
/// </summary>
public sealed class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly IWebHostEnvironment _environment;

    public HangfireDashboardAuthorizationFilter(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Allow anonymous access in Development for easier inspection.
        if (_environment.IsDevelopment())
        {
            return true;
        }

        var user = httpContext.User;
        if (user?.Identity is null || !user.Identity.IsAuthenticated)
        {
            return false;
        }

        return user.IsInRole("SuperAdmin") || user.IsInRole("Admin");
    }
}
