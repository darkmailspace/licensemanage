namespace LicenseManager.Application.Payments.Models;

public enum WebhookProcessOutcome
{
    /// <summary>Event was new, parsed, and applied to local state.</summary>
    Processed,
    /// <summary>(Provider, EventId) already existed - safe no-op.</summary>
    Duplicate,
    /// <summary>Recognised but not actionable (e.g. unsupported event type).</summary>
    Ignored,
    /// <summary>Signature verification failed - the webhook was rejected.</summary>
    SignatureInvalid,
    /// <summary>Parsing or persistence failed.</summary>
    Error,
}

public sealed class WebhookProcessResult
{
    public WebhookProcessOutcome Outcome { get; init; }
    public string? ProviderEventId { get; init; }
    public string? EventType { get; init; }
    public Guid? PaymentId { get; init; }
    public string? Message { get; init; }

    public bool ShouldAck => Outcome != WebhookProcessOutcome.SignatureInvalid;

    public static WebhookProcessResult Processed(string eventId, string eventType, Guid? paymentId)
        => new() { Outcome = WebhookProcessOutcome.Processed, ProviderEventId = eventId, EventType = eventType, PaymentId = paymentId };

    public static WebhookProcessResult Duplicate(string eventId, string eventType)
        => new() { Outcome = WebhookProcessOutcome.Duplicate, ProviderEventId = eventId, EventType = eventType };

    public static WebhookProcessResult Ignored(string eventId, string eventType, string? reason = null)
        => new() { Outcome = WebhookProcessOutcome.Ignored, ProviderEventId = eventId, EventType = eventType, Message = reason };

    public static WebhookProcessResult SignatureInvalid(string? message = null)
        => new() { Outcome = WebhookProcessOutcome.SignatureInvalid, Message = message ?? "Signature verification failed" };

    public static WebhookProcessResult Error(string message)
        => new() { Outcome = WebhookProcessOutcome.Error, Message = message };
}
