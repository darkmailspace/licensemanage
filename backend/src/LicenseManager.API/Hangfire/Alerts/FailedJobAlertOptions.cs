namespace LicenseManager.API.Hangfire.Alerts;

/// <summary>
/// Bound from <c>Hangfire:Alerts</c> in appsettings. Controls throttling and
/// severity thresholds for the default <see cref="LoggingFailedJobAlerter"/>.
/// </summary>
public sealed class FailedJobAlertOptions
{
    public const string SectionName = "Hangfire:Alerts";

    /// <summary>
    /// If true, the LoggingFailedJobAlerter throttles repeat alerts for the
    /// same job-type within <see cref="ThrottleWindowMinutes"/>.
    /// </summary>
    public bool ThrottleEnabled { get; set; } = true;

    /// <summary>
    /// Window during which only one alert per (job-type, queue) pair is
    /// emitted. Repeats inside the window are dropped to a Debug log line.
    /// </summary>
    public int ThrottleWindowMinutes { get; set; } = 10;

    /// <summary>
    /// Job types for which a terminal failure should be logged at Critical
    /// instead of Error. Anything customer-facing belongs here.
    /// </summary>
    public string[] CriticalJobTypes { get; set; } = new[]
    {
        "NotificationQueueProcessorJob",
        "FailedNotificationRetryJob",
    };
}
