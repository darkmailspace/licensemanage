using Hangfire;
using LicenseManager.API.Jobs;
using Microsoft.Extensions.Options;

namespace LicenseManager.API.Hangfire;

/// <summary>
/// Centralised registration of every recurring Hangfire job in the system.
/// Idempotent: <see cref="IRecurringJobManager.AddOrUpdate"/> upserts the
/// schedule on every startup, so changing a cron string in configuration is
/// picked up after a redeploy.
/// </summary>
public static class RecurringJobScheduler
{
    // Public job ids - kept as constants so they can be referenced from the
    // dashboard, tests, and any future "trigger now" admin endpoints.
    public const string LicenseExpiryReminder = "license-expiry-reminder";
    public const string LicenseExpiryWarning30Days = "license-expiry-warning-30d";
    public const string LicenseExpiryWarning7Days = "license-expiry-warning-7d";
    public const string DailyLicenseValidation = "daily-license-validation";
    public const string DailyCleanup = "daily-cleanup";
    public const string AuditLogCleanup = "audit-log-cleanup";
    public const string NotificationQueueProcessor = "notification-queue-processor";
    public const string FailedNotificationRetry = "failed-notification-retry";

    /// <summary>
    /// Registers (or updates) every recurring job. Safe to call on every
    /// application start.
    /// </summary>
    public static void RegisterAll(IServiceProvider services)
    {
        var manager = services.GetRequiredService<IRecurringJobManager>();
        var options = services.GetRequiredService<IOptions<HangfireOptions>>().Value;
        var logger = services.GetRequiredService<ILogger<HangfireMarker>>();

        var crons = options.RecurringJobs;
        var tz = TimeZoneInfo.Utc;

        // ----- expiry reminders / warnings (queue: default) -------------------
        manager.AddOrUpdate<LicenseExpiryReminderJob>(
            LicenseExpiryReminder,
            "default",
            job => job.ExecuteAsync(CancellationToken.None),
            crons.LicenseExpiryReminder,
            new RecurringJobOptions { TimeZone = tz });

        manager.AddOrUpdate<LicenseExpiryWarning30DaysJob>(
            LicenseExpiryWarning30Days,
            "default",
            job => job.ExecuteAsync(CancellationToken.None),
            crons.LicenseExpiryWarning30Days,
            new RecurringJobOptions { TimeZone = tz });

        manager.AddOrUpdate<LicenseExpiryWarning7DaysJob>(
            LicenseExpiryWarning7Days,
            "default",
            job => job.ExecuteAsync(CancellationToken.None),
            crons.LicenseExpiryWarning7Days,
            new RecurringJobOptions { TimeZone = tz });

        // ----- daily housekeeping (queue: low) --------------------------------
        manager.AddOrUpdate<DailyLicenseValidationJob>(
            DailyLicenseValidation,
            "low",
            job => job.ExecuteAsync(CancellationToken.None),
            crons.DailyLicenseValidation,
            new RecurringJobOptions { TimeZone = tz });

        manager.AddOrUpdate<DailyCleanupJob>(
            DailyCleanup,
            "low",
            job => job.ExecuteAsync(CancellationToken.None),
            crons.DailyCleanup,
            new RecurringJobOptions { TimeZone = tz });

        manager.AddOrUpdate<AuditLogCleanupJob>(
            AuditLogCleanup,
            "low",
            job => job.ExecuteAsync(CancellationToken.None),
            crons.AuditLogCleanup,
            new RecurringJobOptions { TimeZone = tz });

        // ----- notifications (queue: critical) --------------------------------
        manager.AddOrUpdate<NotificationQueueProcessorJob>(
            NotificationQueueProcessor,
            "critical",
            job => job.ExecuteAsync(CancellationToken.None),
            crons.NotificationQueueProcessor,
            new RecurringJobOptions { TimeZone = tz });

        manager.AddOrUpdate<FailedNotificationRetryJob>(
            FailedNotificationRetry,
            "critical",
            job => job.ExecuteAsync(CancellationToken.None),
            crons.FailedNotificationRetry,
            new RecurringJobOptions { TimeZone = tz });

        logger.LogInformation(
            "RecurringJobScheduler: registered 8 recurring job(s): {Jobs}",
            new[]
            {
                LicenseExpiryReminder,
                LicenseExpiryWarning30Days,
                LicenseExpiryWarning7Days,
                DailyLicenseValidation,
                DailyCleanup,
                AuditLogCleanup,
                NotificationQueueProcessor,
                FailedNotificationRetry,
            });
    }

    /// <summary>
    /// Marker type used purely so we can resolve a typed
    /// <see cref="ILogger{T}"/> with the "Hangfire" category.
    /// </summary>
    public sealed class HangfireMarker { }
}
