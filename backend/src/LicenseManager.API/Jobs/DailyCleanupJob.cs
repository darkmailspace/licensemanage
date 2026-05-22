using Hangfire;
using LicenseManager.API.Hangfire;
using LicenseManager.API.Hangfire.Retry;
using LicenseManager.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LicenseManager.API.Jobs;

/// <summary>
/// Daily housekeeping: trims rows that grow without bound from high-volume
/// tables (api_logs and license_validations) using their CreatedAt timestamp.
/// AuditLog cleanup runs on its own (longer) cadence in
/// <see cref="AuditLogCleanupJob"/>.
/// </summary>
[BestEffortRetry]
public sealed class DailyCleanupJob
{
    private readonly IApplicationDbContext _db;
    private readonly ILogger<DailyCleanupJob> _logger;
    private readonly IOptionsMonitor<HangfireOptions> _options;

    public DailyCleanupJob(
        IApplicationDbContext db,
        ILogger<DailyCleanupJob> logger,
        IOptionsMonitor<HangfireOptions> options)
    {
        _db = db;
        _logger = logger;
        _options = options;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var retention = _options.CurrentValue.Retention;
        var apiLogCutoff = DateTime.UtcNow.AddDays(-Math.Max(1, retention.ApiLogDays));
        var validationCutoff = DateTime.UtcNow.AddDays(-Math.Max(1, retention.LicenseValidationDays));

        _logger.LogInformation(
            "DailyCleanupJob: api_logs older than {ApiCutoff:O}, license_validations older than {ValCutoff:O}",
            apiLogCutoff, validationCutoff);

        var apiDeleted = await _db.ApiLogs
            .Where(a => a.CreatedAt < apiLogCutoff)
            .ExecuteDeleteAsync(cancellationToken);

        var validationsDeleted = await _db.LicenseValidations
            .Where(v => v.CreatedAt < validationCutoff)
            .ExecuteDeleteAsync(cancellationToken);

        _logger.LogInformation(
            "DailyCleanupJob: deleted {ApiDeleted} api log(s) and {ValDeleted} license validation(s)",
            apiDeleted, validationsDeleted);
    }
}
