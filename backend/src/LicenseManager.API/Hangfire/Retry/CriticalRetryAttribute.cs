using Hangfire;

namespace LicenseManager.API.Hangfire.Retry;

/// <summary>
/// Aggressive retry budget for jobs whose failure has direct customer impact
/// (notification dispatch, retry queue draining). 5 attempts with backoff
/// 30s, 1m, 2m, 5m, 10m; on exhaustion the job moves to FailedState which
/// triggers <see cref="LicenseManager.API.Hangfire.Filters.FailedJobAlertFilter"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class CriticalRetryAttribute : AutomaticRetryAttribute
{
    public CriticalRetryAttribute()
    {
        Attempts = RetryPolicies.Critical.Attempts;
        DelaysInSeconds = RetryPolicies.Critical.DelaysInSeconds;
        OnAttemptsExceeded = AttemptsExceededAction.Fail;
        LogEvents = true;
    }
}
