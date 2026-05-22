using LicenseManager.Application.Common.Interfaces;
using LicenseManager.Application.Payments.Models;
using LicenseManager.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LicenseManager.API.Controllers.Webhooks;

/// <summary>
/// Stripe webhook receiver. Stripe signs the body with the
/// <c>Stripe-Signature</c> header; the gateway verifies it via
/// <c>EventUtility.ConstructEvent</c>.
///
/// Note on security: webhooks are public (anonymous) endpoints. Authentication
/// is via the HMAC signature, not via a bearer token. Anything that gets past
/// signature verification is treated as authentic; anything that fails it is
/// rejected with HTTP 400 - which is what Stripe expects when retrying.
/// </summary>
[ApiController]
[Route("api/webhooks/stripe")]
[AllowAnonymous]
[Produces("application/json")]
public sealed class StripeWebhookController : ControllerBase
{
    private const string SignatureHeader = "Stripe-Signature";

    private readonly IPaymentService _payments;
    private readonly ILogger<StripeWebhookController> _logger;

    public StripeWebhookController(IPaymentService payments, ILogger<StripeWebhookController> logger)
    {
        _payments = payments;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        var (rawBody, signature) = await ReadRawAsync(cancellationToken);

        var result = await _payments.HandleWebhookAsync(
            PaymentProvider.Stripe, rawBody, signature, cancellationToken);

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
                _logger.LogWarning("Stripe webhook rejected: signature invalid");
                return BadRequest(new { error = "signature_invalid" });

            case WebhookProcessOutcome.Error:
                _logger.LogError("Stripe webhook processing error: {Message}", result.Message);
                // 500 -> Stripe retries with exponential backoff.
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
