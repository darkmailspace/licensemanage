using Hangfire;
using Hangfire.States;
using LicenseManager.API.Authorization;
using LicenseManager.API.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LicenseManager.API.Controllers;

/// <summary>
/// Read-only operational view over the Hangfire job system. Backs an admin
/// dashboard (charts, recurring-job state, failed-job triage) without
/// exposing the full Hangfire UI to non-admins.
/// </summary>
[ApiController]
[Route("api/admin/jobs")]
[Authorize(Policy = Policies.Admin)]
[Produces("application/json")]
public sealed class JobMonitoringController : ControllerBase
{
    private readonly JobStorage _storage;

    public JobMonitoringController(JobStorage storage)
    {
        _storage = storage;
    }

    /// <summary>
    /// Top-level dashboard counters: enqueued / processing / succeeded /
    /// failed / scheduled / retries / servers / recurring jobs.
    /// </summary>
    [HttpGet("stats")]
    public ActionResult<ApiResponse<object>> GetStats()
    {
        var monitor = _storage.GetMonitoringApi();
        var stats = monitor.GetStatistics();

        return Ok(ApiResponse<object>.Ok(new
        {
            enqueued = stats.Enqueued,
            processing = stats.Processing,
            succeeded = stats.Succeeded,
            failed = stats.Failed,
            scheduled = stats.Scheduled,
            deleted = stats.Deleted,
            retries = stats.Retries,
            servers = stats.Servers,
            recurring = stats.Recurring,
            queues = stats.Queues,
            timestampUtc = DateTime.UtcNow,
        }));
    }

    /// <summary>
    /// Per-day succeeded / failed history (for charting). Hangfire stores
    /// counters with daily/hourly resolution; we expose the last week.
    /// </summary>
    [HttpGet("history")]
    public ActionResult<ApiResponse<object>> GetHistory()
    {
        var monitor = _storage.GetMonitoringApi();

        var succeeded = monitor.SucceededByDatesCount();
        var failed = monitor.FailedByDatesCount();

        return Ok(ApiResponse<object>.Ok(new
        {
            succeededByDay = succeeded.OrderBy(kv => kv.Key)
                                       .Select(kv => new { date = kv.Key, count = kv.Value }),
            failedByDay = failed.OrderBy(kv => kv.Key)
                                 .Select(kv => new { date = kv.Key, count = kv.Value }),
        }));
    }

    /// <summary>
    /// All registered recurring jobs with their cron, queue, last/next
    /// execution times, and last-job state.
    /// </summary>
    [HttpGet("recurring")]
    public ActionResult<ApiResponse<object>> GetRecurringJobs()
    {
        using var connection = _storage.GetConnection();
        var jobs = connection.GetRecurringJobs();

        var view = jobs.Select(j => new
        {
            id = j.Id,
            cron = j.Cron,
            queue = j.Queue,
            timeZoneId = j.TimeZoneId,
            createdAt = j.CreatedAt,
            lastExecution = j.LastExecution,
            lastJobId = j.LastJobId,
            lastJobState = j.LastJobState,
            nextExecution = j.NextExecution,
            error = j.Error,
        });

        return Ok(ApiResponse<object>.Ok(view));
    }

    /// <summary>
    /// Currently failed jobs (terminal, post-retry). Paged.
    /// </summary>
    [HttpGet("failed")]
    public ActionResult<ApiResponse<object>> GetFailedJobs([FromQuery] int from = 0, [FromQuery] int count = 50)
    {
        if (from < 0) from = 0;
        count = Math.Clamp(count, 1, 200);

        var monitor = _storage.GetMonitoringApi();
        var failed = monitor.FailedJobs(from, count);

        var view = failed.Select(kv => new
        {
            jobId = kv.Key,
            jobType = kv.Value.Job?.Type?.Name,
            method = kv.Value.Job?.Method?.Name,
            queue = kv.Value.Job?.Queue,
            failedAt = kv.Value.FailedAt,
            reason = kv.Value.Reason,
            exceptionType = kv.Value.ExceptionType,
            exceptionMessage = kv.Value.ExceptionMessage,
            inFailedState = kv.Value.InFailedState,
        });

        return Ok(ApiResponse<object>.Ok(new
        {
            from,
            count,
            total = monitor.FailedCount(),
            items = view,
        }));
    }

    /// <summary>
    /// Hangfire workers / servers currently registered with this storage.
    /// </summary>
    [HttpGet("servers")]
    public ActionResult<ApiResponse<object>> GetServers()
    {
        var monitor = _storage.GetMonitoringApi();
        var servers = monitor.Servers();

        var view = servers.Select(s => new
        {
            name = s.Name,
            workersCount = s.WorkersCount,
            queues = s.Queues,
            startedAt = s.StartedAt,
            heartbeat = s.Heartbeat,
        });

        return Ok(ApiResponse<object>.Ok(view));
    }

    /// <summary>
    /// Triggers a recurring job to run immediately. Useful for ops smoke
    /// tests or to recover from a missed schedule.
    /// </summary>
    [HttpPost("recurring/{id}/trigger")]
    public ActionResult<ApiResponse<object>> TriggerRecurring([FromServices] IRecurringJobManager manager, string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(ApiResponse<object>.Fail("Recurring job id is required."));
        }

        manager.Trigger(id);
        return Ok(ApiResponse<object>.Ok(new { id, triggeredAt = DateTime.UtcNow }));
    }

    /// <summary>
    /// Re-enqueues a failed job for another attempt. Bypasses the retry
    /// budget - operator must intentionally request the retry.
    /// </summary>
    [HttpPost("failed/{id}/requeue")]
    public ActionResult<ApiResponse<object>> RequeueFailed([FromServices] IBackgroundJobClient client, string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(ApiResponse<object>.Fail("Job id is required."));
        }

        var ok = client.ChangeState(id, new EnqueuedState(), FailedState.StateName);
        if (!ok)
        {
            return NotFound(ApiResponse<object>.Fail($"Failed job '{id}' not found or not in Failed state."));
        }

        return Ok(ApiResponse<object>.Ok(new { id, requeuedAt = DateTime.UtcNow }));
    }
}
