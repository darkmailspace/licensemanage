using LicenseManager.Domain.Common;
using LicenseManager.Domain.Enums;

namespace LicenseManager.Domain.Entities;

/// <summary>
/// Persisted record of every webhook delivery attempt made by a payment
/// provider. The (Provider, ProviderEventId) pair is unique - this is what
/// gives the webhook handler at-most-once processing semantics under the
/// providers' "we may deliver multiple times" guarantee.
/// </summary>
public class WebhookEvent : BaseEntity
{
    public PaymentProvider Provider { get; set; }

    /// <summary>
    /// Stripe: <c>event.id</c> (evt_xxx). Razorpay: synthesised from
    /// <c>x-razorpay-event-id</c> header when present, else SHA-256 of the
    /// raw body.
    /// </summary>
    public string ProviderEventId { get; set; } = string.Empty;

    /// <summary>e.g. "payment_intent.succeeded", "payment.captured".</summary>
    public string EventType { get; set; } = string.Empty;

    public WebhookEventStatus Status { get; set; } = WebhookEventStatus.Received;

    /// <summary>Raw JSON payload exactly as received (for replay / audit).</summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>Signature header that was verified before persistence.</summary>
    public string? Signature { get; set; }

    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>Optional link to the Payment that this event affected.</summary>
    public Guid? PaymentId { get; set; }
}
