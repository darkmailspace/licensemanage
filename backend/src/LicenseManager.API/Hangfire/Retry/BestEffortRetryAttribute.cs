using Hangfire;

namespace LicenseManager.API.Hangfire.Retry;

/// <summary>
/// Light retry budget for low-priority housekeeping jobs (api log / audit
/// log / license validation cleanup). 2 attempts with backoff 2m and 10m.
/// These jobs run every day, so a missed run is not catastrophic.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class BestEffortRetryAttribute : AutomaticRetryAttribute
{
    public BestEffortRetryAttribute()
    {
        Attempts = RetryPolicies.BestEffort.Attempts;
        DelaysInSeconds = RetryPolicies.BestEffort.DelaysInSeconds;
        OnAttemptsExceeded = AttemptsExceededAction.Fail;
        LogEvents = true;
    }
}
