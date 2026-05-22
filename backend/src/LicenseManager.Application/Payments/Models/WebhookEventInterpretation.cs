using LicenseManager.Domain.Enums;

namespace LicenseManager.Application.Payments.Models;

/// <summary>
/// Provider-agnostic projection of a verified webhook event. Each gateway
/// parses its own JSON shape and produces this; the service applies it to
/// the local <c>Payment</c>/<c>Refund</c> rows without knowing whose JSON
/// it came from.
/// </summary>
public sealed class WebhookEventInterpretation
{
    /// <summary>Stripe pi_xxx / Razorpay pay_xxx, when present in the event.</summary>
    public string? ProviderPaymentId { get; init; }

    /// <summary>Razorpay order_xxx (Stripe events do not carry a separate order id).</summary>
    public string? ProviderOrderId { get; init; }

    /// <summary>New PaymentStatus to apply to the matched Payment row.</summary>
    public PaymentStatus? NewPaymentStatus { get; init; }

    public string? ProviderRefundId { get; init; }
    public RefundStatus? NewRefundStatus { get; init; }
    public long? RefundedAmountMinor { get; init; }

    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    public string? Notes { get; init; }

    public bool IsActionable
        => NewPaymentStatus.HasValue
           || NewRefundStatus.HasValue
           || RefundedAmountMinor.HasValue;

    public static WebhookEventInterpretation Ignored(string? notes = null) => new() { Notes = notes };
}
