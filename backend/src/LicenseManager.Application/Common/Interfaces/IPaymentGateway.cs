using LicenseManager.Application.Payments.Models;
using LicenseManager.Domain.Entities;
using LicenseManager.Domain.Enums;

namespace LicenseManager.Application.Common.Interfaces;

/// <summary>
/// Provider-specific operations. Concrete implementations live in the
/// Infrastructure layer and are selected by <see cref="IPaymentGatewayFactory"/>.
///
/// All implementations are expected to be safe to register as scoped services
/// and to never throw for "expected" failure modes (declined cards, invalid
/// signatures); those return a result with <c>Success = false</c> and a
/// classified error code instead.
/// </summary>
public interface IPaymentGateway
{
    PaymentProvider Provider { get; }

    /// <summary>
    /// Open a new session at the provider. The implementation populates the
    /// supplied <paramref name="payment"/> with provider ids and returns the
    /// client-facing handles. The caller is responsible for persisting the
    /// row.
    /// </summary>
    Task<PaymentSession> CreateSessionAsync(
        Payment payment,
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronously verify a payment using whatever proof the provider's
    /// client returns (Razorpay signature triplet, Stripe PaymentIntent id).
    /// </summary>
    Task<PaymentVerificationResult> VerifyAsync(
        Payment payment,
        PaymentVerificationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Issue a full or partial refund against a captured payment.
    /// </summary>
    Task<RefundResult> RefundAsync(
        Payment payment,
        Refund refund,
        RefundRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify the signature on a webhook delivery and return the parsed
    /// event handle. Implementations must use a constant-time comparison.
    /// </summary>
    /// <param name="rawBody">Body exactly as received - no JSON re-serialisation.</param>
    /// <param name="signatureHeader">Provider-specific signature header.</param>
    /// <param name="parsedEventId">Event id (evt_xxx for Stripe, etc.).</param>
    /// <param name="parsedEventType">Event type (e.g. "payment.captured").</param>
    bool VerifyWebhookSignature(
        string rawBody,
        string? signatureHeader,
        out string parsedEventId,
        out string parsedEventType);

    /// <summary>
    /// Parse a verified webhook body and project it to a provider-agnostic
    /// state delta the service can apply. Must not throw for unknown event
    /// types - return <see cref="WebhookEventInterpretation.Ignored"/>.
    /// </summary>
    WebhookEventInterpretation InterpretWebhook(string rawBody, string eventType);
}
