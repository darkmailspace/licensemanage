namespace LicenseManager.API.Hangfire;

/// <summary>
/// Strongly-typed binding for the "Hangfire" section of appsettings.
/// Drives the recurring-job schedule and the data-retention windows used by
/// the cleanup jobs.
/// </summary>
public sealed class HangfireOptions
{
    public const string SectionName = "Hangfire";

    public string DashboardPath { get; set; } = "/hangfire";
    public int WorkerCount { get; set; } = 0;
    public string[] Queues { get; set; } = new[] { "critical", "default", "low" };
    public string SchemaName { get; set; } = "hangfire";
    public bool PrepareSchemaIfNecessary { get; set; } = true;
    public bool DashboardRequireAuth { get; set; } = true;

    public RecurringJobsOptions RecurringJobs { get; set; } = new();
    public RetentionOptions Retention { get; set; } = new();
    public ExpiryWindowOptions ExpiryWindows { get; set; } = new();

    public sealed class RecurringJobsOptions
    {
        // Cron expressions in UTC.  Defaults:
        //   * Reminder / warnings:    daily, staggered between 09:00 and 09:30
        //   * Daily validation:       02:00 UTC
        //   * Daily cleanup:          03:00 UTC
        //   * Audit log cleanup:      04:00 UTC every Sunday
        //   * Notification queue:     every 5 minutes
        //   * Failed notif retry:     every 30 minutes
        public string LicenseExpiryReminder { get; set; } = "0 9 * * *";
        public string LicenseExpiryWarning30Days { get; set; } = "15 9 * * *";
        public string LicenseExpiryWarning7Days { get; set; } = "30 9 * * *";
        public string DailyLicenseValidation { get; set; } = "0 2 * * *";
        public string DailyCleanup { get; set; } = "0 3 * * *";
        public string AuditLogCleanup { get; set; } = "0 4 * * 0";
        public string NotificationQueueProcessor { get; set; } = "*/5 * * * *";
        public string FailedNotificationRetry { get; set; } = "*/30 * * * *";
    }

    public sealed class RetentionOptions
    {
        public int ApiLogDays { get; set; } = 30;
        public int LicenseValidationDays { get; set; } = 90;
        public int AuditLogDays { get; set; } = 365;
    }

    public sealed class ExpiryWindowOptions
    {
        public int ReminderDays { get; set; } = 60;
        public int Warning30Days { get; set; } = 30;
        public int Warning7Days { get; set; } = 7;
    }
}
