using Hangfire;

namespace LicenseManager.API.Hangfire.Retry;

/// <summary>
/// Default retry budget for routine recurring jobs (daily license sweeps,
/// expiry warnings). 3 attempts with backoff 1m, 5m, 15m.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class StandardRetryAttribute : AutomaticRetryAttribute
{
    public StandardRetryAttribute()
    {
        Attempts = RetryPolicies.Standard.Attempts;
        DelaysInSeconds = RetryPolicies.Standard.DelaysInSeconds;
        OnAttemptsExceeded = AttemptsExceededAction.Fail;
        LogEvents = true;
    }
}
