using LicenseManager.Application.Common.Interfaces;
using LicenseManager.Application.Payments.Models;
using LicenseManager.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LicenseManager.API.Controllers.Webhooks;

/// <summary>
/// Razorpay webhook receiver. Razorpay signs the raw body with the
/// <c>X-Razorpay-Signature</c> header (HMAC-SHA256, hex). Verification is
/// done by the gateway with a constant-time comparison.
/// </summary>
[ApiController]
[Route("api/webhooks/razorpay")]
[AllowAnonymous]
[Produces("application/json")]
public sealed class RazorpayWebhookController : ControllerBase
{
    private const string SignatureHeader = "X-Razorpay-Signature";

    private readonly IPaymentService _payments;
    private readonly ILogger<RazorpayWebhookController> _logger;

    public RazorpayWebhookController(IPaymentService payments, ILogger<RazorpayWebhookController> logger)
    {
        _payments = payments;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        var (rawBody, signature) = await ReadRawAsync(cancellationToken);

        var result = await _payments.HandleWebhookAsync(
            PaymentProvider.Razorpay, rawBody, signature, cancellationToken);

        return MapResult(result);
    }

    private async Task<(string Body, string? Signature)> ReadRawAsync(CancellationToken cancellationToken)
    {
        Request.EnableBuffering();
        Request.Body.Position = 0;
        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync(cancellationToken);
        Request.Body.Position = 0;

        var signature = Request.Headers.TryGetValue(SignatureHeader, out var values)
            ? values.ToString()
            : null;

        return (body, signature);
    }

    private IActionResult MapResult(WebhookProcessResult result)
    {
        switch (result.Outcome)
        {
            case WebhookProcessOutcome.SignatureInvalid:
                _logger.LogWarning("Razorpay webhook rejected: signature invalid");
                return BadRequest(new { error = "signature_invalid" });

            case WebhookProcessOutcome.Error:
                _logger.LogError("Razorpay webhook processing error: {Message}", result.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "processing_error", message = result.Message });

            case WebhookProcessOutcome.Duplicate:
                return Ok(new { received = true, duplicate = true, eventId = result.ProviderEventId });

            case WebhookProcessOutcome.Ignored:
                return Ok(new { received = true, ignored = true, eventId = result.ProviderEventId, eventType = result.EventType, reason = result.Message });

            case WebhookProcessOutcome.Processed:
            default:
                return Ok(new { received = true, eventId = result.ProviderEventId, eventType = result.EventType, paymentId = result.PaymentId });
        }
    }
}
