using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace LicenseManager.API.Hangfire.Alerts;

/// <summary>
/// Default <see cref="IFailedJobAlerter"/>: emits a structured log line at
/// Error (or Critical for jobs in <see cref="FailedJobAlertOptions.CriticalJobTypes"/>),
/// optionally throttled per (job-type, queue) to avoid spam when a job fails
/// in a tight loop.
///
/// Future phases can register additional <see cref="IFailedJobAlerter"/>
/// implementations (email, PagerDuty, webhook) alongside this one.
/// </summary>
public sealed class LoggingFailedJobAlerter : IFailedJobAlerter
{
    private const string CachePrefix = "hangfire-alert:";

    private readonly ILogger<LoggingFailedJobAlerter> _logger;
    private readonly IMemoryCache _cache;
    private readonly IOptionsMonitor<FailedJobAlertOptions> _options;

    public LoggingFailedJobAlerter(
        ILogger<LoggingFailedJobAlerter> logger,
        IMemoryCache cache,
        IOptionsMonitor<FailedJobAlertOptions> options)
    {
        _logger = logger;
        _cache = cache;
        _options = options;
    }

    public void Alert(FailedJobAlert alert)
    {
        var opts = _options.CurrentValue;
        var critical = opts.CriticalJobTypes.Contains(alert.JobType, StringComparer.OrdinalIgnoreCase);

        if (opts.ThrottleEnabled && IsThrottled(alert, opts))
        {
            _logger.LogDebug(
                "FailedJobAlert (throttled): {JobType} on queue {Queue} - alert suppressed within {Window}-minute window",
                alert.JobType, alert.Queue, opts.ThrottleWindowMinutes);
            return;
        }

        var level = critical ? LogLevel.Critical : LogLevel.Error;

        // One log call - structured fields are first-class for downstream
        // log routers (e.g. Serilog -> Loki) to alert on.
        _logger.Log(level,
            "FailedJobAlert: terminal failure - JobType={JobType}, JobId={JobId}, Method={Method}, Queue={Queue}, " +
            "Attempts={Attempt}/{MaxAttempts}, ExceptionType={ExceptionType}, ExceptionMessage={ExceptionMessage}, FailedAt={FailedAt:O}",
            alert.JobType, alert.JobId, alert.MethodName, alert.Queue,
            alert.RetryAttempt, alert.MaxAttempts,
            alert.ExceptionType ?? "(none)",
            alert.ExceptionMessage ?? "(none)",
            alert.FailedAtUtc);
    }

    private bool IsThrottled(FailedJobAlert alert, FailedJobAlertOptions opts)
    {
        var key = $"{CachePrefix}{alert.JobType}:{alert.Queue}";
        if (_cache.TryGetValue(key, out _))
        {
            return true;
        }

        _cache.Set(key, true, TimeSpan.FromMinutes(Math.Max(1, opts.ThrottleWindowMinutes)));
        return false;
    }
}
