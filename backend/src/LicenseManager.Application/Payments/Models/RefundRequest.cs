namespace LicenseManager.Application.Payments.Models;

public sealed class RefundRequest
{
    public Guid PaymentId { get; init; }

    /// <summary>
    /// Amount to refund in major units. If null, refund the remainder
    /// (full refund).
    /// </summary>
    public decimal? Amount { get; init; }

    public string? Reason { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}
