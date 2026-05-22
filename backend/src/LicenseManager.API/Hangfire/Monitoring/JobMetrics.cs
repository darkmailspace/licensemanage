using System.Diagnostics.Metrics;

namespace LicenseManager.API.Hangfire.Monitoring;

/// <summary>
/// OpenTelemetry-compatible metrics for Hangfire job execution. Instruments
/// are created against a single named meter ("LicenseManager.Hangfire") so
/// any standard exporter (Prometheus, OTLP, etc.) added in a future phase
/// can scrape them without further code changes.
/// </summary>
public sealed class JobMetrics
{
    public const string MeterName = "LicenseManager.Hangfire";
    public const string MeterVersion = "1.0.0";

    private readonly Counter<long> _executions;
    private readonly Counter<long> _terminalFailures;
    private readonly Counter<long> _retries;
    private readonly Histogram<double> _durationMs;

    public JobMetrics(IMeterFactory factory)
    {
        var meter = factory.Create(MeterName, MeterVersion);

        _executions = meter.CreateCounter<long>(
            name: "licensemanager.jobs.executions",
            unit: "{execution}",
            description: "Total Hangfire job executions, tagged by job/queue/outcome.");

        _terminalFailures = meter.CreateCounter<long>(
            name: "licensemanager.jobs.failures",
            unit: "{failure}",
            description: "Hangfire jobs that exhausted their retry budget and ended in FailedState.");

        _retries = meter.CreateCounter<long>(
            name: "licensemanager.jobs.retries",
            unit: "{retry}",
            description: "Hangfire job retry attempts (failures that were re-scheduled).");

        _durationMs = meter.CreateHistogram<double>(
            name: "licensemanager.jobs.duration",
            unit: "ms",
            description: "Hangfire job execution duration in milliseconds.");
    }

    public void RecordExecution(string job, string queue, string outcome, double durationMs)
    {
        var tags = new TagList
        {
            { "job", job },
            { "queue", queue },
            { "outcome", outcome },
        };
        _executions.Add(1, tags);
        _durationMs.Record(durationMs, tags);
    }

    public void RecordRetry(string job, string queue)
    {
        _retries.Add(1, new TagList
        {
            { "job", job },
            { "queue", queue },
        });
    }

    public void RecordTerminalFailure(string job, string queue)
    {
        _terminalFailures.Add(1, new TagList
        {
            { "job", job },
            { "queue", queue },
        });
    }
}
