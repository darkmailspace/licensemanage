namespace LicenseManager.Domain.Enums;

/// <summary>
/// Lifecycle of a Payment row.  Modeled as the union of Stripe's PaymentIntent
/// statuses and Razorpay's payment statuses, mapped to a stable internal set.
/// </summary>
public enum PaymentStatus
{
    /// <summary>Local row created; provider session not yet opened.</summary>
    Created = 0,

    /// <summary>Provider session/order opened, awaiting customer action.</summary>
    Pending = 1,

    /// <summary>Provider authorized the payment but it has not been captured yet.</summary>
    Authorized = 2,

    /// <summary>Funds captured / payment successful.</summary>
    Captured = 3,

    /// <summary>Provider reported a terminal failure.</summary>
    Failed = 4,

    /// <summary>Customer or merchant cancelled before capture.</summary>
    Cancelled = 5,

    /// <summary>Captured then fully refunded.</summary>
    Refunded = 6,

    /// <summary>Captured then refunded for a portion of the amount.</summary>
    PartiallyRefunded = 7,
}
