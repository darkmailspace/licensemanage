using Hangfire;
using LicenseManager.API.Hangfire;
using LicenseManager.API.Hangfire.Retry;
using LicenseManager.Application.Common.Interfaces;
using LicenseManager.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LicenseManager.API.Jobs;

/// <summary>
/// 30-day expiry warning. Runs daily; flags every active license whose
/// ExpiryDate lands exactly N days from today (default: 30) so renewal
/// follow-up can be triggered.
/// </summary>
[StandardRetry]
public sealed class LicenseExpiryWarning30DaysJob
{
    private readonly IApplicationDbContext _db;
    private readonly ILogger<LicenseExpiryWarning30DaysJob> _logger;
    private readonly IOptionsMonitor<HangfireOptions> _options;

    public LicenseExpiryWarning30DaysJob(
        IApplicationDbContext db,
        ILogger<LicenseExpiryWarning30DaysJob> logger,
        IOptionsMonitor<HangfireOptions> options)
    {
        _db = db;
        _logger = logger;
        _options = options;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var days = _options.CurrentValue.ExpiryWindows.Warning30Days;
        var today = DateTime.UtcNow.Date;
        var windowStart = today.AddDays(days);
        var windowEnd = windowStart.AddDays(1);

        _logger.LogInformation(
            "LicenseExpiryWarning30DaysJob: scanning licenses expiring on {Date}",
            windowStart);

        var matches = await _db.Licenses
            .AsNoTracking()
            .Where(l => !l.IsDeleted)
            .Where(l => l.Status == LicenseStatus.Active)
            .Where(l => l.ExpiryDate >= windowStart && l.ExpiryDate < windowEnd)
            .Select(l => new { l.Id, l.LicenseKey, l.CustomerId, l.ExpiryDate })
            .ToListAsync(cancellationToken);

        _logger.LogWarning(
            "LicenseExpiryWarning30DaysJob: {Count} license(s) expire in {Days} days",
            matches.Count, days);

        foreach (var lic in matches)
        {
            // TODO: dispatch via notification subsystem (Phase 4B.4).
            _logger.LogWarning(
                "30-day warning: license {LicenseId} ({LicenseKey}) for customer {CustomerId} expires on {ExpiryDate:O}",
                lic.Id, lic.LicenseKey, lic.CustomerId, lic.ExpiryDate);
        }
    }
}
