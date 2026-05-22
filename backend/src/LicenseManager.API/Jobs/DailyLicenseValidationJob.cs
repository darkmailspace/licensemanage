using Hangfire;
using LicenseManager.API.Hangfire.Retry;
using LicenseManager.Application.Common.Interfaces;
using LicenseManager.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LicenseManager.API.Jobs;

/// <summary>
/// Sweeps the license table once per day to keep statuses honest:
///   * Active licenses past ExpiryDate but inside their grace window are moved
///     to GracePeriod (StartGracePeriod is invoked the first time they cross).
///   * Active licenses past ExpiryDate without a grace window (or whose grace
///     window has elapsed) are moved to Expired.
///   * Already-GracePeriod licenses whose grace has elapsed are moved to
///     Expired.
/// </summary>
[StandardRetry]
public sealed class DailyLicenseValidationJob
{
    private readonly IApplicationDbContext _db;
    private readonly ILogger<DailyLicenseValidationJob> _logger;

    public DailyLicenseValidationJob(
        IApplicationDbContext db,
        ILogger<DailyLicenseValidationJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        _logger.LogInformation("DailyLicenseValidationJob: starting sweep at {Now:O}", now);

        // Anything that is Active or already in GracePeriod and past ExpiryDate
        // is a candidate for transition. Keep this server-side (no IsExpired()
        // call - that's a CLR method EF can't translate).
        var candidates = await _db.Licenses
            .Where(l => !l.IsDeleted)
            .Where(l => l.Status == LicenseStatus.Active || l.Status == LicenseStatus.GracePeriod)
            .Where(l => l.ExpiryDate < now)
            .ToListAsync(cancellationToken);

        var transitionedToGrace = 0;
        var transitionedToExpired = 0;

        foreach (var license in candidates)
        {
            if (license.Status == LicenseStatus.Active)
            {
                if (license.GracePeriodDays > 0)
                {
                    license.StartGracePeriod();
                    transitionedToGrace++;
                    _logger.LogInformation(
                        "License {LicenseId} ({LicenseKey}): Active -> GracePeriod (grace ends {End:O})",
                        license.Id, license.LicenseKey,
                        license.GracePeriodStartDate?.AddDays(license.GracePeriodDays));
                }
                else
                {
                    license.Status = LicenseStatus.Expired;
                    license.UpdatedAt = now;
                    transitionedToExpired++;
                    _logger.LogInformation(
                        "License {LicenseId} ({LicenseKey}): Active -> Expired",
                        license.Id, license.LicenseKey);
                }
            }
            else // already in GracePeriod
            {
                var graceEnd = (license.GracePeriodStartDate ?? license.ExpiryDate)
                    .AddDays(license.GracePeriodDays);

                if (now > graceEnd)
                {
                    license.Status = LicenseStatus.Expired;
                    license.InGracePeriod = false;
                    license.UpdatedAt = now;
                    transitionedToExpired++;
                    _logger.LogInformation(
                        "License {LicenseId} ({LicenseKey}): GracePeriod -> Expired (grace ended {End:O})",
                        license.Id, license.LicenseKey, graceEnd);
                }
            }
        }

        if (candidates.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "DailyLicenseValidationJob: scanned {Total} candidate(s); -> grace: {Grace}, -> expired: {Expired}",
            candidates.Count, transitionedToGrace, transitionedToExpired);
    }
}
