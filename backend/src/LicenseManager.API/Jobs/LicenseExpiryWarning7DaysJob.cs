using Hangfire;
using LicenseManager.API.Hangfire;
using LicenseManager.API.Hangfire.Retry;
using LicenseManager.Application.Common.Interfaces;
using LicenseManager.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LicenseManager.API.Jobs;

/// <summary>
/// 7-day urgent expiry warning. Runs daily; flags every active license whose
/// ExpiryDate lands exactly N days from today (default: 7).
/// </summary>
[StandardRetry]
public sealed class LicenseExpiryWarning7DaysJob
{
    private readonly IApplicationDbContext _db;
    private readonly ILogger<LicenseExpiryWarning7DaysJob> _logger;
    private readonly IOptionsMonitor<HangfireOptions> _options;

    public LicenseExpiryWarning7DaysJob(
        IApplicationDbContext db,
        ILogger<LicenseExpiryWarning7DaysJob> logger,
        IOptionsMonitor<HangfireOptions> options)
    {
        _db = db;
        _logger = logger;
        _options = options;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var days = _options.CurrentValue.ExpiryWindows.Warning7Days;
        var today = DateTime.UtcNow.Date;
        var windowStart = today.AddDays(days);
        var windowEnd = windowStart.AddDays(1);

        _logger.LogInformation(
            "LicenseExpiryWarning7DaysJob: scanning licenses expiring on {Date}",
            windowStart);

        var matches = await _db.Licenses
            .AsNoTracking()
            .Where(l => !l.IsDeleted)
            .Where(l => l.Status == LicenseStatus.Active)
            .Where(l => l.ExpiryDate >= windowStart && l.ExpiryDate < windowEnd)
            .Select(l => new { l.Id, l.LicenseKey, l.CustomerId, l.ExpiryDate })
            .ToListAsync(cancellationToken);

        _logger.LogWarning(
            "LicenseExpiryWarning7DaysJob: {Count} license(s) expire in {Days} days",
            matches.Count, days);

        foreach (var lic in matches)
        {
            // TODO: dispatch via notification subsystem (Phase 4B.4).
            _logger.LogWarning(
                "7-day warning: license {LicenseId} ({LicenseKey}) for customer {CustomerId} expires on {ExpiryDate:O}",
                lic.Id, lic.LicenseKey, lic.CustomerId, lic.ExpiryDate);
        }
    }
}
