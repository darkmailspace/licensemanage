using LicenseManager.Application.Payments.Models;
using LicenseManager.Domain.Entities;

namespace LicenseManager.Application.Common.Interfaces;

/// <summary>
/// High-level orchestrator over <see cref="IPaymentGateway"/>. Owns the
/// provider-agnostic workflow: persist <c>Payment</c>, call gateway, persist
/// updated state, and turn webhook deliveries into local-state changes.
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Create a new local Payment row, open a session at the chosen provider,
    /// persist the row, and return the client-facing handles.
    /// </summary>
    Task<PaymentSession> CreatePaymentAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify a payment that the customer has just completed in the provider's
    /// checkout. Updates the local Payment row to Captured/Failed.
    /// </summary>
    Task<PaymentVerificationResult> VerifyPaymentAsync(
        PaymentVerificationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Issue a refund. Creates a <c>Refund</c> row, calls the gateway, and
    /// updates the parent payment's RefundedAmount/Status accordingly.
    /// </summary>
    Task<RefundResult> RefundPaymentAsync(
        RefundRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Look up a Payment by id.</summary>
    Task<Payment?> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate, persist, and apply a webhook delivery. Idempotent on the
    /// (Provider, ProviderEventId) pair.
    /// </summary>
    Task<WebhookProcessResult> HandleWebhookAsync(
        Domain.Enums.PaymentProvider provider,
        string rawBody,
        string? signatureHeader,
        CancellationToken cancellationToken = default);
}
