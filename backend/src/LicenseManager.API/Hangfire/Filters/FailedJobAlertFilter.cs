using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using LicenseManager.API.Hangfire.Alerts;
using LicenseManager.API.Hangfire.Monitoring;

namespace LicenseManager.API.Hangfire.Filters;

/// <summary>
/// Detects terminal job failures (FailedState that survived
/// <c>AutomaticRetryAttribute</c>'s state election) and dispatches a
/// <see cref="FailedJobAlert"/> through every registered
/// <see cref="IFailedJobAlerter"/>. Also bumps the terminal-failure counter
/// in <see cref="JobMetrics"/>.
///
/// Why this only fires on terminal failures: <c>AutomaticRetryAttribute</c>
/// implements <c>IElectStateFilter</c> and replaces FailedState with
/// ScheduledState while attempts remain. By the time
/// <see cref="OnStateApplied"/> runs with a FailedState, retries have been
/// exhausted (or the job has no retry policy at all).
/// </summary>
public sealed class FailedJobAlertFilter : JobFilterAttribute, IApplyStateFilter
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly JobMetrics _metrics;
    private readonly ILogger<FailedJobAlertFilter> _logger;

    public FailedJobAlertFilter(
        IServiceScopeFactory scopeFactory,
        JobMetrics metrics,
        ILogger<FailedJobAlertFilter> logger)
    {
        _scopeFactory = scopeFactory;
        _metrics = metrics;
        _logger = logger;
        Order = 100;
    }

    public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
        if (context.NewState is not FailedState failed)
        {
            return;
        }

        var job = context.BackgroundJob.Job;
        var jobType = job.Type.Name;
        var queue = job.Queue ?? "default";
        var attempt = ReadRetryCount(context.Connection, context.BackgroundJob.Id);
        var maxAttempts = ResolveMaxAttempts(job);

        _metrics.RecordTerminalFailure(jobType, queue);

        var alert = new FailedJobAlert(
            JobId: context.BackgroundJob.Id,
            JobType: jobType,
            MethodName: job.Method.Name,
            Queue: queue,
            RetryAttempt: attempt,
            MaxAttempts: maxAttempts,
            ExceptionType: failed.Exception?.GetType().FullName,
            ExceptionMessage: failed.Exception?.Message,
            ExceptionStackTrace: failed.Exception?.StackTrace,
            FailedAtUtc: failed.FailedAt);

        DispatchSafely(alert);
    }

    public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
        // No-op; we only act on Failed state being applied.
    }

    private void DispatchSafely(FailedJobAlert alert)
    {
        // Filters run inside Hangfire's state-application transaction. We
        // never want an alerter exception to corrupt that transaction, so
        // every dispatch is isolated and any throw is swallowed and logged.
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var alerters = scope.ServiceProvider.GetServices<IFailedJobAlerter>().ToArray();

            if (alerters.Length == 0)
            {
                _logger.LogDebug(
                    "FailedJobAlertFilter: no IFailedJobAlerter registered; alert for {JobType} suppressed",
                    alert.JobType);
                return;
            }

            foreach (var alerter in alerters)
            {
                try
                {
                    alerter.Alert(alert);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "FailedJobAlertFilter: alerter {Alerter} threw while handling alert for {JobType}",
                        alerter.GetType().Name, alert.JobType);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "FailedJobAlertFilter: failed to dispatch alert for {JobType} (jobId={JobId})",
                alert.JobType, alert.JobId);
        }
    }

    private static int ReadRetryCount(IStorageConnection connection, string jobId)
    {
        var raw = connection.GetJobParameter(jobId, "RetryCount");
        return int.TryParse(raw, out var n) ? n : 0;
    }

    private static int ResolveMaxAttempts(Job job)
    {
        // Walk both class-level and method-level retry attributes so jobs
        // that ship their own [AutomaticRetry(...)] are reflected accurately.
        var attrs = job.Method
            .GetCustomAttributes(typeof(AutomaticRetryAttribute), inherit: true)
            .Concat(job.Type.GetCustomAttributes(typeof(AutomaticRetryAttribute), inherit: true))
            .OfType<AutomaticRetryAttribute>()
            .ToArray();

        return attrs.Length > 0 ? attrs.Max(a => a.Attempts) : 0;
    }
}
