using LicenseManager.Domain.Enums;

namespace LicenseManager.Application.Payments.Models;

/// <summary>
/// What the gateway hands back after a session is opened. The API surfaces
/// this DTO to clients so the frontend can drive provider-specific checkout
/// flows (Stripe Elements with ClientSecret, or Razorpay Checkout with the
/// OrderId + key).
/// </summary>
public sealed class PaymentSession
{
    public Guid PaymentId { get; init; }
    public PaymentProvider Provider { get; init; }
    public PaymentStatus Status { get; init; }
    public long AmountMinor { get; init; }
    public string Currency { get; init; } = "USD";

    /// <summary>Stripe: pi_xxx. Razorpay: pay_xxx (null until customer completes).</summary>
    public string? ProviderPaymentId { get; init; }

    /// <summary>Razorpay only: order_xxx, passed to Razorpay Checkout JS.</summary>
    public string? ProviderOrderId { get; init; }

    /// <summary>Stripe: PaymentIntent client_secret (frontend uses to confirm).</summary>
    public string? ClientSecret { get; init; }

    /// <summary>Hosted-checkout URL when the provider returns one.</summary>
    public string? CheckoutUrl { get; init; }

    /// <summary>Public key the frontend needs to talk to the provider directly.</summary>
    public string? PublishableKey { get; init; }

    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}
