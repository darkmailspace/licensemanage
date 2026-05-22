using LicenseManager.Domain.Common;
using LicenseManager.Domain.Enums;

namespace LicenseManager.Domain.Entities;

public class Refund : BaseEntity
{
    public Guid PaymentId { get; set; }

    /// <summary>Stripe refund id (re_xxx) / Razorpay refund id (rfnd_xxx).</summary>
    public string? ProviderRefundId { get; set; }

    public decimal Amount { get; set; }
    public long AmountMinor { get; set; }
    public string Currency { get; set; } = "USD";

    public RefundStatus Status { get; set; } = RefundStatus.Pending;
    public string? Reason { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? RefundedAt { get; set; }

    public string? RawProviderData { get; set; }

    public virtual Payment? Payment { get; set; }
}
