namespace LicenseManager.API.Hangfire.Alerts;

/// <summary>
/// Snapshot of a Hangfire job that has exhausted its retry budget. Passed to
/// every registered <see cref="IFailedJobAlerter"/> when a job transitions
/// terminally into FailedState.
/// </summary>
public sealed record FailedJobAlert(
    string JobId,
    string JobType,
    string MethodName,
    string Queue,
    int RetryAttempt,
    int MaxAttempts,
    string? ExceptionType,
    string? ExceptionMessage,
    string? ExceptionStackTrace,
    DateTime FailedAtUtc);
