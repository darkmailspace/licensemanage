using Hangfire;
using LicenseManager.API.Hangfire.Retry;

namespace LicenseManager.API.Jobs;

/// <summary>
/// Drains the outbound notification queue.
///
/// PHASE 4B.2 NOTE: the persistent notification entity / dispatcher subsystem
/// is scheduled for a later phase (4B.4). The job class, schedule slot, and
/// retry policy are wired up now so the scheduler is complete and stable;
/// the body is intentionally a no-op that emits a structured trace log so
/// operators can confirm the job is firing on its 5-minute cadence.
/// </summary>
[CriticalRetry]
[DisableConcurrentExecution(timeoutInSeconds: 60)]
public sealed class NotificationQueueProcessorJob
{
    private readonly ILogger<NotificationQueueProcessorJob> _logger;

    public NotificationQueueProcessorJob(ILogger<NotificationQueueProcessorJob> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogTrace(
            "NotificationQueueProcessorJob: tick at {Now:O} - notification subsystem not yet enabled (Phase 4B.4)",
            DateTime.UtcNow);

        // TODO (Phase 4B.4):
        //   1. Pull a batch (e.g. 100) of pending notifications from the
        //      Notifications table ordered by CreatedAt.
        //   2. Dispatch via the registered INotificationDispatcher (email/SMS).
        //   3. Mark each row as Sent/Failed and persist.

        return Task.CompletedTask;
    }
}
