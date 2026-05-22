using Hangfire;
using LicenseManager.API.Hangfire.Retry;

namespace LicenseManager.API.Jobs;

/// <summary>
/// Periodically retries notifications that previously failed delivery.
///
/// PHASE 4B.2 NOTE: like <see cref="NotificationQueueProcessorJob"/>, this job
/// has its scheduler entry and retry attribute defined now so the operations
/// surface (cron + dashboard tile) is complete; the actual retry logic lands
/// once the Notifications entity exists (Phase 4B.4).
/// </summary>
[CriticalRetry]
[DisableConcurrentExecution(timeoutInSeconds: 60)]
public sealed class FailedNotificationRetryJob
{
    private readonly ILogger<FailedNotificationRetryJob> _logger;

    public FailedNotificationRetryJob(ILogger<FailedNotificationRetryJob> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogTrace(
            "FailedNotificationRetryJob: tick at {Now:O} - notification subsystem not yet enabled (Phase 4B.4)",
            DateTime.UtcNow);

        // TODO (Phase 4B.4):
        //   1. Find Notifications where Status == Failed AND
        //      RetryCount < MaxRetries AND NextRetryAt <= UtcNow.
        //   2. Re-dispatch via INotificationDispatcher with exponential backoff.
        //   3. Update RetryCount/NextRetryAt/Status accordingly.

        return Task.CompletedTask;
    }
}
