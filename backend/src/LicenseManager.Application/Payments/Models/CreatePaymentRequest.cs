using LicenseManager.Domain.Enums;

namespace LicenseManager.Application.Payments.Models;

/// <summary>
/// Request to open a new payment session with a provider. The service layer
/// turns this into a <c>Payment</c> row and a provider-side artefact
/// (Stripe PaymentIntent / Razorpay Order).
/// </summary>
public sealed class CreatePaymentRequest
{
    public PaymentProvider Provider { get; init; }
    public Guid CustomerId { get; init; }
    public Guid? LicenseId { get; init; }

    /// <summary>Amount in major units (e.g. 49.99 USD, 4999 INR).</summary>
    public decimal Amount { get; init; }

    /// <summary>ISO 4217 currency code (USD, INR, ...).</summary>
    public string Currency { get; init; } = "USD";

    public string? Description { get; init; }
    public string? Receipt { get; init; }
    public string? CustomerEmail { get; init; }
    public string? CustomerName { get; init; }
    public string? CustomerPhone { get; init; }

    /// <summary>Free-form metadata passed through to the provider verbatim.</summary>
    public Dictionary<string, string>? Metadata { get; init; }
}
