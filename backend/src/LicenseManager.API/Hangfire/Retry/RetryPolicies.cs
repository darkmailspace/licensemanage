namespace LicenseManager.API.Hangfire.Retry;

/// <summary>
/// Centralised retry budgets for recurring/background jobs. Each named profile
/// is exposed as a Hangfire <c>AutomaticRetryAttribute</c> subclass so jobs
/// can declare intent semantically (<c>[CriticalRetry]</c>) instead of
/// hardcoding magic numbers.
///
/// Total wall-clock retry windows (sum of delays):
///   * Critical    ~18.5 min  (5 attempts)  - notifications, anything customer-visible
///   * Standard    ~21.0 min  (3 attempts)  - daily license sweeps and warnings
///   * BestEffort  ~12.0 min  (2 attempts)  - low-priority cleanup jobs
/// </summary>
public static class RetryPolicies
{
    public static class Critical
    {
        public const int Attempts = 5;
        public static readonly int[] DelaysInSeconds = { 30, 60, 120, 300, 600 };
    }

    public static class Standard
    {
        public const int Attempts = 3;
        public static readonly int[] DelaysInSeconds = { 60, 300, 900 };
    }

    public static class BestEffort
    {
        public const int Attempts = 2;
        public static readonly int[] DelaysInSeconds = { 120, 600 };
    }
}
