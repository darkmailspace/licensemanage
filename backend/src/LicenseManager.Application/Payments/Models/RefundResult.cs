using LicenseManager.Domain.Enums;

namespace LicenseManager.Application.Payments.Models;

// RefundResult uses 'record' (not 'class') so PaymentService can return
// `result with { RefundId = refund.Id }` after persistence assigns the
// generated refund id. The 'with' operator is record-only.
public sealed record RefundResult
{
    public bool Success { get; init; }
    public Guid RefundId { get; init; }
    public Guid PaymentId { get; init; }
    public string? ProviderRefundId { get; init; }
    public RefundStatus Status { get; init; }
    public long AmountMinor { get; init; }
    public string Currency { get; init; } = "USD";
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
}
