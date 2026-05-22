using Microsoft.AspNetCore.Authorization;

namespace LicenseManager.API.Authorization;

/// <summary>
/// Policy names. Lower numeric role = higher privilege.
/// SuperAdmin=1, Admin=2, Support=3, Viewer=4
/// </summary>
public static class Policies
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Support = "Support";
    public const string Authenticated = "Authenticated";
    public const string AdminOrSupport = "AdminOrSupport";
}

public static class AuthorizationExtensions
{
    public static AuthorizationOptions AddLicenseManagerPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(Policies.SuperAdmin, p =>
            p.RequireAuthenticatedUser()
             .RequireRole("1"));

        options.AddPolicy(Policies.Admin, p =>
            p.RequireAuthenticatedUser()
             .RequireAssertion(ctx =>
                 int.TryParse(ctx.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value, out var r)
                 && r <= 2));

        options.AddPolicy(Policies.AdminOrSupport, p =>
            p.RequireAuthenticatedUser()
             .RequireAssertion(ctx =>
                 int.TryParse(ctx.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value, out var r)
                 && r <= 3));

        options.AddPolicy(Policies.Support, p =>
            p.RequireAuthenticatedUser()
             .RequireAssertion(ctx =>
                 int.TryParse(ctx.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value, out var r)
                 && r <= 3));

        options.AddPolicy(Policies.Authenticated, p =>
            p.RequireAuthenticatedUser());

        return options;
    }
}
