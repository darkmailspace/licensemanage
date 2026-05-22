namespace LicenseManager.API.Hangfire.Alerts;

/// <summary>
/// Sink for terminal Hangfire job failures (failed after retries exhausted).
/// Implementations are expected to be cheap and non-throwing - the filter
/// invokes them inside Hangfire's state-application transaction.
/// </summary>
public interface IFailedJobAlerter
{
    void Alert(FailedJobAlert alert);
}
