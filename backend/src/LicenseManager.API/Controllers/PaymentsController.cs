using LicenseManager.API.Authorization;
using LicenseManager.API.Common;
using LicenseManager.Application.Common.Interfaces;
using LicenseManager.Application.Payments.Models;
using LicenseManager.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LicenseManager.API.Controllers;

/// <summary>
/// Authenticated endpoints for managing payments. Provider selection is a
/// request parameter so a single front-end can offer Stripe (e.g. for
/// non-IN customers) and Razorpay (for IN) side-by-side.
/// </summary>
[ApiController]
[Route("api/payments")]
[Authorize(Policy = Policies.Authenticated)]
[Produces("application/json")]
public sealed class PaymentsController : ControllerBase
{
    private readonly IPaymentService _payments;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IPaymentService payments, ILogger<PaymentsController> logger)
    {
        _payments = payments;
        _logger = logger;
    }

    /// <summary>
    /// Open a new payment session at the chosen provider. Returns the
    /// handles the frontend needs to drive checkout (Stripe ClientSecret /
    /// Razorpay OrderId + KeyId).
    /// </summary>
    [HttpPost("session")]
    public async Task<ActionResult<ApiResponse<PaymentSession>>> CreateSession(
        [FromBody] CreatePaymentSessionDto body,
        CancellationToken cancellationToken)
    {
        if (body is null)
        {
            return BadRequest(ApiResponse<PaymentSession>.Fail("Body is required."));
        }

        if (body.Amount <= 0)
        {
            return BadRequest(ApiResponse<PaymentSession>.Fail("Amount must be greater than zero."));
        }

        if (body.CustomerId == Guid.Empty)
        {
            return BadRequest(ApiResponse<PaymentSession>.Fail("CustomerId is required."));
        }

        try
        {
            var session = await _payments.CreatePaymentAsync(new CreatePaymentRequest
            {
                Provider = body.Provider,
                CustomerId = body.CustomerId,
                LicenseId = body.LicenseId,
                Amount = body.Amount,
                Currency = string.IsNullOrWhiteSpace(body.Currency) ? "USD" : body.Currency,
                Description = body.Description,
                Receipt = body.Receipt,
                CustomerEmail = body.CustomerEmail,
                CustomerName = body.CustomerName,
                CustomerPhone = body.CustomerPhone,
                Metadata = body.Metadata,
            }, cancellationToken);

            return Ok(ApiResponse<PaymentSession>.Ok(session));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Refused to create payment session: {Reason}", ex.Message);
            return UnprocessableEntity(ApiResponse<PaymentSession>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Synchronously verify a checkout that the customer has just completed.
    /// Razorpay: pass <c>order_id / payment_id / signature</c>. Stripe: pass
    /// the PaymentIntent id.
    /// </summary>
    [HttpPost("{paymentId:guid}/verify")]
    public async Task<ActionResult<ApiResponse<PaymentVerificationResult>>> Verify(
        Guid paymentId,
        [FromBody] VerifyPaymentDto body,
        CancellationToken cancellationToken)
    {
        if (body is null)
        {
            return BadRequest(ApiResponse<PaymentVerificationResult>.Fail("Body is required."));
        }

        try
        {
            var result = await _payments.VerifyPaymentAsync(new PaymentVerificationRequest
            {
                PaymentId = paymentId,
                ProviderOrderId = body.ProviderOrderId,
                ProviderPaymentId = body.ProviderPaymentId,
                Signature = body.Signature,
            }, cancellationToken);

            return result.Success
                ? Ok(ApiResponse<PaymentVerificationResult>.Ok(result))
                : UnprocessableEntity(ApiResponse<PaymentVerificationResult>.Fail(result.ErrorMessage ?? "Verification failed."));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(ApiResponse<PaymentVerificationResult>.Fail(ex.Message));
        }
    }

    /// <summary>Issue a full or partial refund.</summary>
    [HttpPost("{paymentId:guid}/refund")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<ActionResult<ApiResponse<RefundResult>>> Refund(
        Guid paymentId,
        [FromBody] RefundDto body,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _payments.RefundPaymentAsync(new RefundRequest
            {
                PaymentId = paymentId,
                Amount = body?.Amount,
                Reason = body?.Reason,
                Metadata = body?.Metadata,
            }, cancellationToken);

            return result.Success
                ? Ok(ApiResponse<RefundResult>.Ok(result))
                : UnprocessableEntity(ApiResponse<RefundResult>.Fail(result.ErrorMessage ?? "Refund failed."));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(ApiResponse<RefundResult>.Fail(ex.Message));
        }
    }

    /// <summary>Look up a payment (with refunds) by id.</summary>
    [HttpGet("{paymentId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Get(Guid paymentId, CancellationToken cancellationToken)
    {
        var payment = await _payments.GetPaymentAsync(paymentId, cancellationToken);
        if (payment is null)
        {
            return NotFound(ApiResponse<object>.Fail($"Payment '{paymentId}' not found."));
        }

        return Ok(ApiResponse<object>.Ok(new
        {
            id = payment.Id,
            customerId = payment.CustomerId,
            licenseId = payment.LicenseId,
            provider = payment.Provider.ToString(),
            providerPaymentId = payment.ProviderPaymentId,
            providerOrderId = payment.ProviderOrderId,
            amount = payment.Amount,
            amountMinor = payment.AmountMinor,
            currency = payment.Currency,
            status = payment.Status.ToString(),
            description = payment.Description,
            receipt = payment.Receipt,
            authorizedAt = payment.AuthorizedAt,
            capturedAt = payment.CapturedAt,
            failedAt = payment.FailedAt,
            cancelledAt = payment.CancelledAt,
            refundedAmountMinor = payment.RefundedAmountMinor,
            errorCode = payment.ErrorCode,
            errorMessage = payment.ErrorMessage,
            createdAt = payment.CreatedAt,
            updatedAt = payment.UpdatedAt,
            refunds = payment.Refunds.Select(r => new
            {
                id = r.Id,
                providerRefundId = r.ProviderRefundId,
                amount = r.Amount,
                amountMinor = r.AmountMinor,
                currency = r.Currency,
                status = r.Status.ToString(),
                reason = r.Reason,
                refundedAt = r.RefundedAt,
                createdAt = r.CreatedAt,
            }),
        }));
    }
}

// ---------------------------------------------------------------------------
// Inbound DTOs (controller-level, kept here for cohesion)
// ---------------------------------------------------------------------------

public sealed class CreatePaymentSessionDto
{
    public PaymentProvider Provider { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? LicenseId { get; set; }
    public decimal Amount { get; set; }
    public string? Currency { get; set; }
    public string? Description { get; set; }
    public string? Receipt { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public sealed class VerifyPaymentDto
{
    public string? ProviderOrderId { get; set; }
    public string? ProviderPaymentId { get; set; }
    public string? Signature { get; set; }
}

public sealed class RefundDto
{
    public decimal? Amount { get; set; }
    public string? Reason { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}
