using System.Diagnostics;
using Hangfire.Server;

namespace LicenseManager.API.Hangfire.Filters;

/// <summary>
/// Wraps every server-side job execution with a structured begin/end log
/// pair. Emits a single Information log on success (with duration) and a
/// single Warning log on transient failure - terminal failures are alerted
/// separately by <see cref="FailedJobAlertFilter"/>.
/// </summary>
public sealed class JobLoggingFilter : IServerFilter
{
    private const string StartTimestampKey = "__lm_start_ts";

    private readonly ILogger<JobLoggingFilter> _logger;

    public JobLoggingFilter(ILogger<JobLoggingFilter> logger)
    {
        _logger = logger;
    }

    public void OnPerforming(PerformingContext filterContext)
    {
        filterContext.Items[StartTimestampKey] = Stopwatch.GetTimestamp();

        _logger.LogInformation(
            "Hangfire job starting: {JobType}.{Method} (jobId={JobId}, queue={Queue})",
            filterContext.BackgroundJob.Job.Type.Name,
            filterContext.BackgroundJob.Job.Method.Name,
            filterContext.BackgroundJob.Id,
            filterContext.BackgroundJob.Job.Queue ?? "default");
    }

    public void OnPerformed(PerformedContext filterContext)
    {
        var elapsed = filterContext.Items.TryGetValue(StartTimestampKey, out var raw) && raw is long start
            ? Stopwatch.GetElapsedTime(start)
            : TimeSpan.Zero;

        var jobType = filterContext.BackgroundJob.Job.Type.Name;
        var method = filterContext.BackgroundJob.Job.Method.Name;
        var jobId = filterContext.BackgroundJob.Id;
        var queue = filterContext.BackgroundJob.Job.Queue ?? "default";

        if (filterContext.Exception is not null)
        {
            _logger.LogWarning(
                filterContext.Exception,
                "Hangfire job threw: {JobType}.{Method} (jobId={JobId}, queue={Queue}) after {ElapsedMs:F1} ms - Hangfire will decide retry vs fail",
                jobType, method, jobId, queue, elapsed.TotalMilliseconds);
        }
        else
        {
            _logger.LogInformation(
                "Hangfire job completed: {JobType}.{Method} (jobId={JobId}, queue={Queue}) in {ElapsedMs:F1} ms",
                jobType, method, jobId, queue, elapsed.TotalMilliseconds);
        }
    }
}
