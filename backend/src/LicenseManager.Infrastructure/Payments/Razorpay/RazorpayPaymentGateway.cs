using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LicenseManager.Application.Common.Interfaces;
using LicenseManager.Application.Payments.Models;
using LicenseManager.Domain.Entities;
using LicenseManager.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LicenseManager.Infrastructure.Payments.Razorpay;

/// <summary>
/// Razorpay implementation of <see cref="IPaymentGateway"/> built on a
/// typed <see cref="HttpClient"/>. The Authorization header (HTTP Basic
/// over <c>key_id:key_secret</c>) is configured at DI registration time;
/// HMAC verification is done with <see cref="CryptographicOperations.FixedTimeEquals"/>
/// so a slow-equal response cannot leak the secret.
///
/// Razorpay flow:
///   1. <c>CreateSession</c> -> POST /v1/orders, returns an order_xxx.
///   2. Frontend Razorpay Checkout JS uses (key_id, order_id) and posts
///      back (order_id, payment_id, signature) on success.
///   3. <c>Verify</c> recomputes HMAC_SHA256(order_id|payment_id, key_secret)
///      in hex and compares to the signature.
///   4. Webhooks: HMAC_SHA256(rawBody, webhook_secret) hex == X-Razorpay-Signature.
/// </summary>
public sealed class RazorpayPaymentGateway : IPaymentGateway
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly RazorpayOptions _options;
    private readonly ILogger<RazorpayPaymentGateway> _logger;

    public RazorpayPaymentGateway(
        HttpClient http,
        IOptions<PaymentOptions> options,
        ILogger<RazorpayPaymentGateway> logger)
    {
        _http = http;
        _options = options.Value.Razorpay;
        _logger = logger;
    }

    public PaymentProvider Provider => PaymentProvider.Razorpay;

    // ---------------------------------------------------------------------
    // Session creation
    // ---------------------------------------------------------------------
    public async Task<PaymentSession> CreateSessionAsync(
        Payment payment,
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var amountMinor = MoneyUtil.ToMinor(request.Amount, request.Currency);
        var receipt = string.IsNullOrWhiteSpace(request.Receipt)
            ? $"lm-{payment.Id:N}"[..Math.Min(40, $"lm-{payment.Id:N}".Length)]
            : request.Receipt;

        var body = new
        {
            amount = amountMinor,
            currency = request.Currency.ToUpperInvariant(),
            receipt,
            notes = request.Metadata ?? new Dictionary<string, string>(),
        };

        using var response = await _http.PostAsJsonAsync("/v1/orders", body, JsonOpts, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Razorpay CreateOrder failed: {Status} {Body}", (int)response.StatusCode, raw);

            payment.MarkFailed(DateTime.UtcNow, $"razorpay_http_{(int)response.StatusCode}", raw);
            payment.RawProviderData = raw;

            throw new InvalidOperationException(
                $"Razorpay rejected order creation ({(int)response.StatusCode}). See payment.ErrorMessage for details.");
        }

        var order = JsonSerializer.Deserialize<RazorpayOrderResponse>(raw, JsonOpts)
            ?? throw new InvalidOperationException("Razorpay returned an empty order response.");

        payment.Provider = Provider;
        payment.ProviderOrderId = order.Id;
        payment.AmountMinor = order.Amount;
        payment.Amount = MoneyUtil.ToMajor(order.Amount, request.Currency);
        payment.Currency = request.Currency.ToUpperInvariant();
        payment.Receipt = order.Receipt;
        payment.Status = PaymentStatus.Pending;
        payment.Description = request.Description;
        payment.RawProviderData = raw;
        payment.UpdatedAt = DateTime.UtcNow;

        return new PaymentSession
        {
            PaymentId = payment.Id,
            Provider = Provider,
            Status = payment.Status,
            AmountMinor = payment.AmountMinor,
            Currency = payment.Currency,
            ProviderOrderId = order.Id,
            PublishableKey = _options.KeyId,
        };
    }

    // ---------------------------------------------------------------------
    // Synchronous verify (Razorpay Checkout success callback)
    // ---------------------------------------------------------------------
    public Task<PaymentVerificationResult> VerifyAsync(
        Payment payment,
        PaymentVerificationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ProviderOrderId)
            || string.IsNullOrWhiteSpace(request.ProviderPaymentId)
            || string.IsNullOrWhiteSpace(request.Signature))
        {
            return Task.FromResult(PaymentVerificationResult.Fail(
                payment.Id, "razorpay_missing_fields", "order_id, payment_id and signature are all required."));
        }

        if (!string.IsNullOrEmpty(payment.ProviderOrderId)
            && !string.Equals(payment.ProviderOrderId, request.ProviderOrderId, StringComparison.Ordinal))
        {
            return Task.FromResult(PaymentVerificationResult.Fail(
                payment.Id, "razorpay_order_mismatch", "order_id does not match the local payment record."));
        }

        var expected = ComputeHmacHex($"{request.ProviderOrderId}|{request.ProviderPaymentId}", _options.KeySecret);
        if (!ConstantTimeEquals(expected, request.Signature))
        {
            payment.MarkFailed(DateTime.UtcNow, "razorpay_signature_invalid", "Signature verification failed.");
            return Task.FromResult(PaymentVerificationResult.Fail(
                payment.Id, "razorpay_signature_invalid", "Signature verification failed."));
        }

        payment.ProviderPaymentId = request.ProviderPaymentId;
        payment.MarkCaptured(DateTime.UtcNow);

        return Task.FromResult(PaymentVerificationResult.Ok(payment.Id, payment.Status, request.ProviderPaymentId));
    }

    // ---------------------------------------------------------------------
    // Refund
    // ---------------------------------------------------------------------
    public async Task<RefundResult> RefundAsync(
        Payment payment,
        Refund refund,
        RefundRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        if (string.IsNullOrWhiteSpace(payment.ProviderPaymentId))
        {
            return new RefundResult
            {
                Success = false,
                PaymentId = payment.Id,
                Status = RefundStatus.Failed,
                ErrorCode = "razorpay_no_payment_id",
                ErrorMessage = "Payment has not been captured yet, nothing to refund.",
            };
        }

        var amountMinor = request.Amount.HasValue
            ? MoneyUtil.ToMinor(request.Amount.Value, payment.Currency)
            : payment.AmountMinor - payment.RefundedAmountMinor;

        var body = new
        {
            amount = amountMinor,
            speed = "normal",
            notes = request.Metadata ?? new Dictionary<string, string>(),
            receipt = $"refund-{refund.Id:N}",
        };

        using var response = await _http.PostAsJsonAsync(
            $"/v1/payments/{payment.ProviderPaymentId}/refund", body, JsonOpts, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Razorpay refund failed for {PaymentId}: {Status} {Body}",
                payment.Id, (int)response.StatusCode, raw);

            return new RefundResult
            {
                Success = false,
                PaymentId = payment.Id,
                RefundId = refund.Id,
                Status = RefundStatus.Failed,
                AmountMinor = amountMinor,
                Currency = payment.Currency,
                ErrorCode = $"razorpay_http_{(int)response.StatusCode}",
                ErrorMessage = raw,
            };
        }

        var parsed = JsonSerializer.Deserialize<RazorpayRefundResponse>(raw, JsonOpts)
            ?? throw new InvalidOperationException("Razorpay returned an empty refund response.");

        var status = MapRefundStatus(parsed.Status);

        refund.ProviderRefundId = parsed.Id;
        refund.AmountMinor = parsed.Amount;
        refund.Amount = MoneyUtil.ToMajor(parsed.Amount, payment.Currency);
        refund.Currency = payment.Currency;
        refund.Status = status;
        refund.RawProviderData = raw;
        refund.RefundedAt = status == RefundStatus.Succeeded ? DateTime.UtcNow : null;
        refund.UpdatedAt = DateTime.UtcNow;

        return new RefundResult
        {
            Success = status != RefundStatus.Failed,
            PaymentId = payment.Id,
            RefundId = refund.Id,
            ProviderRefundId = parsed.Id,
            Status = status,
            AmountMinor = parsed.Amount,
            Currency = payment.Currency,
        };
    }

    // ---------------------------------------------------------------------
    // Webhook signature + interpretation
    // ---------------------------------------------------------------------
    public bool VerifyWebhookSignature(
        string rawBody,
        string? signatureHeader,
        out string parsedEventId,
        out string parsedEventType)
    {
        parsedEventId = string.Empty;
        parsedEventType = string.Empty;

        if (string.IsNullOrWhiteSpace(signatureHeader) || string.IsNullOrEmpty(_options.WebhookSecret))
        {
            return false;
        }

        var expected = ComputeHmacHex(rawBody, _options.WebhookSecret);
        if (!ConstantTimeEquals(expected, signatureHeader))
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(rawBody);
            if (doc.RootElement.TryGetProperty("event", out var evt))
            {
                parsedEventType = evt.GetString() ?? string.Empty;
            }
            // Razorpay does not include a top-level event_id; SHA-256 of the body
            // gives a stable per-delivery identifier the dedup table can key on.
            parsedEventId = Sha256Hex(rawBody);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public WebhookEventInterpretation InterpretWebhook(string rawBody, string eventType)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawBody);
            var payload = doc.RootElement.GetProperty("payload");

            return eventType switch
            {
                "payment.authorized" => InterpretPayment(payload, PaymentStatus.Authorized),
                "payment.captured" => InterpretPayment(payload, PaymentStatus.Captured),
                "payment.failed" => InterpretPayment(payload, PaymentStatus.Failed),
                "order.paid" => InterpretOrder(payload, PaymentStatus.Captured),
                "refund.created" => InterpretRefund(payload, RefundStatus.Pending),
                "refund.processed" => InterpretRefund(payload, RefundStatus.Succeeded),
                "refund.failed" => InterpretRefund(payload, RefundStatus.Failed),
                _ => WebhookEventInterpretation.Ignored($"Unhandled Razorpay event '{eventType}'."),
            };
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Failed to interpret Razorpay webhook (event '{Event}')", eventType);
            return WebhookEventInterpretation.Ignored($"Unparseable Razorpay event '{eventType}'.");
        }
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------
    private static WebhookEventInterpretation InterpretPayment(JsonElement payload, PaymentStatus status)
    {
        var entity = payload.GetProperty("payment").GetProperty("entity");
        var paymentId = entity.GetProperty("id").GetString();
        var orderId = entity.TryGetProperty("order_id", out var oid) ? oid.GetString() : null;
        var errorCode = entity.TryGetProperty("error_code", out var ec) ? ec.GetString() : null;
        var errorMsg = entity.TryGetProperty("error_description", out var em) ? em.GetString() : null;

        return new WebhookEventInterpretation
        {
            ProviderPaymentId = paymentId,
            ProviderOrderId = orderId,
            NewPaymentStatus = status,
            ErrorCode = errorCode,
            ErrorMessage = errorMsg,
        };
    }

    private static WebhookEventInterpretation InterpretOrder(JsonElement payload, PaymentStatus status)
    {
        var orderEntity = payload.GetProperty("order").GetProperty("entity");
        var orderId = orderEntity.GetProperty("id").GetString();

        // order.paid also includes the payment payload alongside the order.
        string? paymentId = null;
        if (payload.TryGetProperty("payment", out var paymentNode)
            && paymentNode.TryGetProperty("entity", out var paymentEntity))
        {
            paymentId = paymentEntity.GetProperty("id").GetString();
        }

        return new WebhookEventInterpretation
        {
            ProviderPaymentId = paymentId,
            ProviderOrderId = orderId,
            NewPaymentStatus = status,
        };
    }

    private static WebhookEventInterpretation InterpretRefund(JsonElement payload, RefundStatus status)
    {
        var refundEntity = payload.GetProperty("refund").GetProperty("entity");
        var refundId = refundEntity.GetProperty("id").GetString();
        var paymentId = refundEntity.GetProperty("payment_id").GetString();
        var amount = refundEntity.GetProperty("amount").GetInt64();

        return new WebhookEventInterpretation
        {
            ProviderPaymentId = paymentId,
            ProviderRefundId = refundId,
            NewRefundStatus = status,
            RefundedAmountMinor = amount,
        };
    }

    private static RefundStatus MapRefundStatus(string? raw) => raw switch
    {
        "processed" => RefundStatus.Succeeded,
        "failed" => RefundStatus.Failed,
        "pending" or "created" or null => RefundStatus.Pending,
        _ => RefundStatus.Pending,
    };

    private static string ComputeHmacHex(string message, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Sha256Hex(string body)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(body));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static bool ConstantTimeEquals(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
        {
            return false;
        }

        var ba = Encoding.UTF8.GetBytes(a);
        var bb = Encoding.UTF8.GetBytes(b);
        return ba.Length == bb.Length && CryptographicOperations.FixedTimeEquals(ba, bb);
    }

    private void EnsureConfigured()
    {
        if (!_options.IsConfigured)
        {
            throw new InvalidOperationException(
                "Razorpay is not configured. Populate Payments:Razorpay (KeyId/KeySecret/WebhookSecret).");
        }
    }

    // --- DTOs for Razorpay REST responses --------------------------------
    private sealed record RazorpayOrderResponse(string Id, long Amount, string Currency, string? Receipt, string Status);

    private sealed record RazorpayRefundResponse(string Id, long Amount, string Currency, string? Status, string? PaymentId);
}
