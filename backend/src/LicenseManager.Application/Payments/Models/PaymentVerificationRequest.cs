namespace LicenseManager.Application.Payments.Models;

/// <summary>
/// Synchronous post-checkout verification payload.
///
/// Razorpay: client returns <c>razorpay_order_id</c>, <c>razorpay_payment_id</c>,
/// <c>razorpay_signature</c> from the Checkout JS handler. We recompute the
/// HMAC and compare in constant time.
///
/// Stripe: clients normally rely on webhooks, but a synchronous check using
/// the PaymentIntent id (passed as <see cref="ProviderPaymentId"/>) is also
/// supported - the gateway just retrieves the PaymentIntent and reads its
/// status.
/// </summary>
public sealed class PaymentVerificationRequest
{
    public Guid PaymentId { get; init; }

    public string? ProviderOrderId { get; init; }
    public string? ProviderPaymentId { get; init; }

    /// <summary>Razorpay HMAC signature returned by Checkout JS.</summary>
    public string? Signature { get; init; }
}
