using LicenseManager.Domain.Enums;

namespace LicenseManager.Application.Payments.Models;

public sealed class PaymentVerificationResult
{
    public bool Success { get; init; }
    public Guid PaymentId { get; init; }
    public PaymentStatus Status { get; init; }
    public string? ProviderPaymentId { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    public static PaymentVerificationResult Ok(Guid paymentId, PaymentStatus status, string? providerPaymentId)
        => new() { Success = true, PaymentId = paymentId, Status = status, ProviderPaymentId = providerPaymentId };

    public static PaymentVerificationResult Fail(Guid paymentId, string code, string message)
        => new() { Success = false, PaymentId = paymentId, Status = PaymentStatus.Failed, ErrorCode = code, ErrorMessage = message };
}
