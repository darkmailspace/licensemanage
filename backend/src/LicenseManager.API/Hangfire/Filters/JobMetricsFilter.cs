using System.Diagnostics;
using Hangfire.Server;
using LicenseManager.API.Hangfire.Monitoring;

namespace LicenseManager.API.Hangfire.Filters;

/// <summary>
/// Records per-execution metrics (count + duration histogram) into
/// <see cref="JobMetrics"/>. Outcome tags are <c>succeeded</c> when the job
/// returns cleanly and <c>failed</c> when it throws (including transient
/// failures that will be retried).
/// </summary>
public sealed class JobMetricsFilter : IServerFilter
{
    private const string StartTimestampKey = "__lm_metrics_start_ts";

    private readonly JobMetrics _metrics;

    public JobMetricsFilter(JobMetrics metrics)
    {
        _metrics = metrics;
    }

    public void OnPerforming(PerformingContext filterContext)
    {
        filterContext.Items[StartTimestampKey] = Stopwatch.GetTimestamp();
    }

    public void OnPerformed(PerformedContext filterContext)
    {
        var elapsedMs = filterContext.Items.TryGetValue(StartTimestampKey, out var raw) && raw is long start
            ? Stopwatch.GetElapsedTime(start).TotalMilliseconds
            : 0d;

        var job = filterContext.BackgroundJob.Job.Type.Name;
        var queue = filterContext.BackgroundJob.Job.Queue ?? "default";
        var outcome = filterContext.Exception is null ? "succeeded" : "failed";

        _metrics.RecordExecution(job, queue, outcome, elapsedMs);
    }
}
