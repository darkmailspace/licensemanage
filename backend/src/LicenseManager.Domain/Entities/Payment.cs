using LicenseManager.Domain.Common;
using LicenseManager.Domain.Enums;

namespace LicenseManager.Domain.Entities;

/// <summary>
/// A single payment attempt against an external provider (Stripe / Razorpay).
/// One Customer can have many Payments; a Payment may optionally be tied to
/// a specific License (e.g. for a renewal or upgrade).
/// </summary>
public class Payment : BaseEntity
{
    // --- Linkage ---------------------------------------------------------
    public Guid CustomerId { get; set; }
    public Guid? LicenseId { get; set; }

    // --- Provider --------------------------------------------------------
    public PaymentProvider Provider { get; set; }

    /// <summary>
    /// Stripe: PaymentIntent id (e.g. pi_xxx).
    /// Razorpay: payment id (e.g. pay_xxx) - filled in only after the customer
    /// completes checkout; while the order is open this stays null.
    /// </summary>
    public string? ProviderPaymentId { get; set; }

    /// <summary>
    /// Razorpay only: the order id (order_xxx) we generated server-side and
    /// passed to the checkout. Used for signature verification.
    /// </summary>
    public string? ProviderOrderId { get; set; }

    /// <summary>Optional: provider-side customer id for vault/billing reuse.</summary>
    public string? ProviderCustomerId { get; set; }

    // --- Money -----------------------------------------------------------
    /// <summary>Amount in major units (e.g. dollars, rupees).</summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Amount in the provider's minor unit (cents/paise). This is the
    /// authoritative field for reconciliation - all providers transact in
    /// minor units and rounding is unambiguous here.
    /// </summary>
    public long AmountMinor { get; set; }

    /// <summary>ISO 4217 currency code (USD, INR, ...).</summary>
    public string Currency { get; set; } = "USD";

    // --- Lifecycle -------------------------------------------------------
    public PaymentStatus Status { get; set; } = PaymentStatus.Created;

    public DateTime? AuthorizedAt { get; set; }
    public DateTime? CapturedAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    /// <summary>Total refunded amount (minor units). Mirrors Status transitions.</summary>
    public long RefundedAmountMinor { get; set; }

    // --- Customer-facing handles ----------------------------------------
    /// <summary>Stripe PaymentIntent client_secret - frontend uses this to confirm.</summary>
    public string? ClientSecret { get; set; }

    /// <summary>Hosted checkout URL when the provider returns one.</summary>
    public string? CheckoutUrl { get; set; }

    /// <summary>Merchant-facing receipt id for matching on bank statements.</summary>
    public string? Receipt { get; set; }

    public string? Description { get; set; }

    // --- Diagnostics -----------------------------------------------------
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>Last raw provider response (JSON), kept for audit/debugging.</summary>
    public string? RawProviderData { get; set; }

    /// <summary>Free-form key/value metadata propagated to the provider.</summary>
    public Dictionary<string, string>? Metadata { get; set; }

    // --- Navigation ------------------------------------------------------
    public virtual Customer? Customer { get; set; }
    public virtual License? License { get; set; }
    public virtual ICollection<Refund> Refunds { get; set; } = new List<Refund>();

    // --- Behaviour -------------------------------------------------------
    public bool IsTerminal()
        => Status is PaymentStatus.Captured
                  or PaymentStatus.Failed
                  or PaymentStatus.Cancelled
                  or PaymentStatus.Refunded;

    public void MarkAuthorized(DateTime utcNow)
    {
        Status = PaymentStatus.Authorized;
        AuthorizedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public void MarkCaptured(DateTime utcNow)
    {
        Status = PaymentStatus.Captured;
        CapturedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public void MarkFailed(DateTime utcNow, string? code, string? message)
    {
        Status = PaymentStatus.Failed;
        FailedAt = utcNow;
        ErrorCode = code;
        ErrorMessage = message;
        UpdatedAt = utcNow;
    }

    public void MarkCancelled(DateTime utcNow)
    {
        Status = PaymentStatus.Cancelled;
        CancelledAt = utcNow;
        UpdatedAt = utcNow;
    }

    public void ApplyRefund(long refundedMinor, DateTime utcNow)
    {
        RefundedAmountMinor = Math.Min(AmountMinor, RefundedAmountMinor + refundedMinor);
        Status = RefundedAmountMinor >= AmountMinor
            ? PaymentStatus.Refunded
            : PaymentStatus.PartiallyRefunded;
        UpdatedAt = utcNow;
    }
}
