using System.Diagnostics;
using LicenseManager.Application.Common.Interfaces;
using LicenseManager.Domain.Entities;

namespace LicenseManager.API.Middleware;

/// <summary>
/// Records every API request to the api_logs table for audit and diagnostics.
/// Skips noisy paths (health, swagger, static).
/// </summary>
public class ApiLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiLoggingMiddleware> _logger;

    private static readonly HashSet<string> SkippedPathPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health", "/swagger", "/_framework", "/favicon.ico",
    };

    public ApiLoggingMiddleware(RequestDelegate next, ILogger<ApiLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IApplicationDbContext db)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var skip = SkippedPathPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        if (skip)
        {
            await _next(context);
            return;
        }

        var sw = Stopwatch.StartNew();
        string? errorMessage = null;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            throw;
        }
        finally
        {
            sw.Stop();

            try
            {
                db.ApiLogs.Add(new ApiLog
                {
                    Endpoint = path,
                    HttpMethod = context.Request.Method,
                    StatusCode = context.Response.StatusCode,
                    ResponseTimeMs = (int)sw.ElapsedMilliseconds,
                    IPAddress = context.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = context.Request.Headers.UserAgent.ToString(),
                    ErrorMessage = errorMessage,
                });
                await db.SaveChangesAsync(context.RequestAborted);
            }
            catch (Exception logEx)
            {
                _logger.LogWarning(logEx, "Failed to write API log for {Path}", path);
            }
        }
    }
}
