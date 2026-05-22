using Hangfire;
using LicenseManager.API.Hangfire;
using LicenseManager.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LicenseManager.API.Jobs;

/// <summary>
/// Weekly cleanup of the audit_logs table. Audit data is sensitive and is kept
/// longer than transient API logs (default: 365 days), so it lives in its own
/// job with its own retention knob.
/// </summary>
[AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 300, 1800 })]
public sealed class AuditLogCleanupJob
{
    private readonly IApplicationDbContext _db;
    private readonly ILogger<AuditLogCleanupJob> _logger;
    private readonly IOptionsMonitor<HangfireOptions> _options;

    public AuditLogCleanupJob(
        IApplicationDbContext db,
        ILogger<AuditLogCleanupJob> logger,
        IOptionsMonitor<HangfireOptions> options)
    {
        _db = db;
        _logger = logger;
        _options = options;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var days = Math.Max(30, _options.CurrentValue.Retention.AuditLogDays);
        var cutoff = DateTime.UtcNow.AddDays(-days);

        _logger.LogInformation(
            "AuditLogCleanupJob: deleting audit_logs older than {Cutoff:O} ({Days}-day retention)",
            cutoff, days);

        var deleted = await _db.AuditLogs
            .Where(a => a.CreatedAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

        _logger.LogInformation("AuditLogCleanupJob: deleted {Deleted} audit log row(s)", deleted);
    }
}
