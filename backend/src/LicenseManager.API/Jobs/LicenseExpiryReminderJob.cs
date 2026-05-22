using Hangfire;
using LicenseManager.API.Hangfire;
using LicenseManager.API.Hangfire.Retry;
using LicenseManager.Application.Common.Interfaces;
using LicenseManager.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LicenseManager.API.Jobs;

/// <summary>
/// Sends an early reminder for licenses expiring in the configured "reminder"
/// window (default: 60 days). Runs once per day; matches licenses whose
/// ExpiryDate falls on the day exactly N days from today (UTC).
/// </summary>
[StandardRetry]
public sealed class LicenseExpiryReminderJob
{
    private readonly IApplicationDbContext _db;
    private readonly ILogger<LicenseExpiryReminderJob> _logger;
    private readonly IOptionsMonitor<HangfireOptions> _options;

    public LicenseExpiryReminderJob(
        IApplicationDbContext db,
        ILogger<LicenseExpiryReminderJob> logger,
        IOptionsMonitor<HangfireOptions> options)
    {
        _db = db;
        _logger = logger;
        _options = options;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var days = _options.CurrentValue.ExpiryWindows.ReminderDays;
        var today = DateTime.UtcNow.Date;
        var windowStart = today.AddDays(days);
        var windowEnd = windowStart.AddDays(1);

        _logger.LogInformation(
            "LicenseExpiryReminderJob: scanning licenses expiring on {Date} ({Days}-day reminder)",
            windowStart, days);

        var matches = await _db.Licenses
            .AsNoTracking()
            .Where(l => !l.IsDeleted)
            .Where(l => l.Status == LicenseStatus.Active)
            .Where(l => l.ExpiryDate >= windowStart && l.ExpiryDate < windowEnd)
            .Select(l => new { l.Id, l.LicenseKey, l.CustomerId, l.ExpiryDate })
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "LicenseExpiryReminderJob: found {Count} license(s) at the {Days}-day mark",
            matches.Count, days);

        foreach (var lic in matches)
        {
            // TODO: enqueue a notification once the notification subsystem is
            // wired up (Phase 4B.4). For now we just emit a structured log line
            // so operators can see the reminder being raised.
            _logger.LogInformation(
                "Reminder ({Days}d): license {LicenseId} ({LicenseKey}) for customer {CustomerId} expires on {ExpiryDate:O}",
                days, lic.Id, lic.LicenseKey, lic.CustomerId, lic.ExpiryDate);
        }
    }
}
