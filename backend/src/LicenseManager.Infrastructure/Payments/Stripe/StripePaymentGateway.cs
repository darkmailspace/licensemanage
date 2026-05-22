using LicenseManager.Application.Common.Interfaces;
using LicenseManager.Application.Payments.Models;
using LicenseManager.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;

// Resolve the name collision between LicenseManager.Domain.Entities.Refund
// and Stripe.Refund. Domain refunds use the unqualified name; Stripe SDK
// types are accessed through these aliases.
using DomainPayment = LicenseManager.Domain.Entities.Payment;
using DomainRefund = LicenseManager.Domain.Entities.Refund;
using StripeEvent = Stripe.Event;
using StripeRefund = Stripe.Refund;

namespace LicenseManager.Infrastructure.Payments.Stripe;

/// <summary>
/// Stripe implementation of <see cref="IPaymentGateway"/> built on the
/// official <c>Stripe.net</c> SDK. Uses the modern PaymentIntent flow:
///   1. Server creates a PaymentIntent and returns its <c>client_secret</c>.
///   2. Frontend (Stripe Elements / Payment Element) confirms with that secret.
///   3. We learn the outcome via webhooks (payment_intent.succeeded etc.)
///      OR by retrieving the PaymentIntent on demand from
///      <see cref="VerifyAsync"/>.
///
/// Webhook signature verification delegates to
/// <see cref="EventUtility.ConstructEvent(string, string, string, long, bool)"/>
/// which already implements the Stripe-Signature scheme (timestamp + v1=...).
/// </summary>
public sealed class StripePaymentGateway : IPaymentGateway
{
    private readonly IStripeClient _stripeClient;
    private readonly StripeOptions _options;
    private readonly ILogger<StripePaymentGateway> _logger;

    public StripePaymentGateway(
        IStripeClient stripeClient,
        IOptions<PaymentOptions> options,
        ILogger<StripePaymentGateway> logger)
    {
        _stripeClient = stripeClient;
        _options = options.Value.Stripe;
        _logger = logger;
    }

    public PaymentProvider Provider => PaymentProvider.Stripe;

    // ---------------------------------------------------------------------
    // Session creation: PaymentIntent
    // ---------------------------------------------------------------------
    public async Task<PaymentSession> CreateSessionAsync(
        DomainPayment payment,
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var amountMinor = MoneyUtil.ToMinor(request.Amount, request.Currency);
        var metadata = BuildMetadata(payment, request);

        var options = new PaymentIntentCreateOptions
        {
            Amount = amountMinor,
            Currency = request.Currency.ToLowerInvariant(),
            Description = request.Description,
            ReceiptEmail = request.CustomerEmail,
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true,
            },
            Metadata = metadata,
        };

        var service = new PaymentIntentService(_stripeClient);
        PaymentIntent intent;
        try
        {
            intent = await service.CreateAsync(options, cancellationToken: cancellationToken);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex,
                "Stripe PaymentIntent.Create failed: {Code} {Message}",
                ex.StripeError?.Code, ex.Message);

            payment.MarkFailed(DateTime.UtcNow, ex.StripeError?.Code ?? "stripe_error", ex.Message);
            throw new InvalidOperationException("Stripe rejected PaymentIntent creation.", ex);
        }

        payment.Provider = Provider;
        payment.ProviderPaymentId = intent.Id;
        payment.ClientSecret = intent.ClientSecret;
        payment.AmountMinor = intent.Amount;
        payment.Amount = MoneyUtil.ToMajor(intent.Amount, intent.Currency);
        payment.Currency = intent.Currency.ToUpperInvariant();
        payment.Status = MapStatus(intent.Status);
        payment.Description = request.Description;
        payment.RawProviderData = intent.ToJson();
        payment.UpdatedAt = DateTime.UtcNow;

        return new PaymentSession
        {
            PaymentId = payment.Id,
            Provider = Provider,
            Status = payment.Status,
            AmountMinor = payment.AmountMinor,
            Currency = payment.Currency,
            ProviderPaymentId = intent.Id,
            ClientSecret = intent.ClientSecret,
            PublishableKey = _options.PublishableKey,
        };
    }

    // ---------------------------------------------------------------------
    // Synchronous verify: re-fetch the PaymentIntent and reflect its status.
    // ---------------------------------------------------------------------
    public async Task<PaymentVerificationResult> VerifyAsync(
        DomainPayment payment,
        PaymentVerificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var providerPaymentId = request.ProviderPaymentId
            ?? payment.ProviderPaymentId;

        if (string.IsNullOrWhiteSpace(providerPaymentId))
        {
            return PaymentVerificationResult.Fail(
                payment.Id, "stripe_missing_payment_intent", "ProviderPaymentId (PaymentIntent id) is required.");
        }

        var service = new PaymentIntentService(_stripeClient);
        PaymentIntent intent;
        try
        {
            intent = await service.GetAsync(providerPaymentId, cancellationToken: cancellationToken);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex,
                "Stripe PaymentIntent.Get failed for {Id}: {Code} {Message}",
                providerPaymentId, ex.StripeError?.Code, ex.Message);
            return PaymentVerificationResult.Fail(
                payment.Id, ex.StripeError?.Code ?? "stripe_error", ex.Message);
        }

        var newStatus = MapStatus(intent.Status);
        var now = DateTime.UtcNow;
        switch (newStatus)
        {
            case PaymentStatus.Captured: payment.MarkCaptured(now); break;
            case PaymentStatus.Authorized: payment.MarkAuthorized(now); break;
            case PaymentStatus.Failed: payment.MarkFailed(now, intent.LastPaymentError?.Code, intent.LastPaymentError?.Message); break;
            case PaymentStatus.Cancelled: payment.MarkCancelled(now); break;
            default: payment.Status = newStatus; payment.UpdatedAt = now; break;
        }
        payment.RawProviderData = intent.ToJson();

        return PaymentVerificationResult.Ok(payment.Id, payment.Status, intent.Id);
    }

    // ---------------------------------------------------------------------
    // Refund
    // ---------------------------------------------------------------------
    public async Task<RefundResult> RefundAsync(
        DomainPayment payment,
        DomainRefund refund,
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
                ErrorCode = "stripe_no_payment_intent",
                ErrorMessage = "Payment has no PaymentIntent id; cannot refund.",
            };
        }

        var amountMinor = request.Amount.HasValue
            ? MoneyUtil.ToMinor(request.Amount.Value, payment.Currency)
            : payment.AmountMinor - payment.RefundedAmountMinor;

        var refundOptions = new RefundCreateOptions
        {
            PaymentIntent = payment.ProviderPaymentId,
            Amount = amountMinor,
            Reason = MapReason(request.Reason),
            Metadata = request.Metadata ?? new Dictionary<string, string>(),
        };

        var service = new RefundService(_stripeClient);
        StripeRefund stripeRefund;
        try
        {
            stripeRefund = await service.CreateAsync(refundOptions, cancellationToken: cancellationToken);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex,
                "Stripe Refund.Create failed for {Intent}: {Code} {Message}",
                payment.ProviderPaymentId, ex.StripeError?.Code, ex.Message);

            return new RefundResult
            {
                Success = false,
                PaymentId = payment.Id,
                RefundId = refund.Id,
                Status = RefundStatus.Failed,
                AmountMinor = amountMinor,
                Currency = payment.Currency,
                ErrorCode = ex.StripeError?.Code ?? "stripe_error",
                ErrorMessage = ex.Message,
            };
        }

        var status = MapRefundStatus(stripeRefund.Status);

        refund.ProviderRefundId = stripeRefund.Id;
        refund.AmountMinor = stripeRefund.Amount;
        refund.Amount = MoneyUtil.ToMajor(stripeRefund.Amount, payment.Currency);
        refund.Currency = payment.Currency;
        refund.Status = status;
        refund.RawProviderData = stripeRefund.ToJson();
        refund.RefundedAt = status == RefundStatus.Succeeded ? DateTime.UtcNow : null;
        refund.UpdatedAt = DateTime.UtcNow;

        return new RefundResult
        {
            Success = status != RefundStatus.Failed,
            PaymentId = payment.Id,
            RefundId = refund.Id,
            ProviderRefundId = stripeRefund.Id,
            Status = status,
            AmountMinor = stripeRefund.Amount,
            Currency = payment.Currency,
        };
    }

    // ---------------------------------------------------------------------
    // Webhook signature verification
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

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                rawBody,
                signatureHeader,
                _options.WebhookSecret,
                throwOnApiVersionMismatch: false);

            parsedEventId = stripeEvent.Id;
            parsedEventType = stripeEvent.Type;
            return true;
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Stripe webhook signature verification failed");
            return false;
        }
    }

    public WebhookEventInterpretation InterpretWebhook(string rawBody, string eventType)
    {
        try
        {
            // We accept that the body has already been validated; reparse here
            // to keep the gateway free of in-memory state between Verify/Interpret.
            var stripeEvent = EventUtility.ParseEvent(rawBody, throwOnApiVersionMismatch: false);

            return eventType switch
            {
                "payment_intent.succeeded" =>
                    InterpretPaymentIntent(stripeEvent, PaymentStatus.Captured),
                "payment_intent.amount_capturable_updated" =>
                    InterpretPaymentIntent(stripeEvent, PaymentStatus.Authorized),
                "payment_intent.payment_failed" =>
                    InterpretPaymentIntent(stripeEvent, PaymentStatus.Failed),
                "payment_intent.canceled" =>
                    InterpretPaymentIntent(stripeEvent, PaymentStatus.Cancelled),
                "charge.refunded" =>
                    InterpretChargeRefunded(stripeEvent),
                _ => WebhookEventInterpretation.Ignored($"Unhandled Stripe event '{eventType}'."),
            };
        }
        catch (Exception ex) when (ex is StripeException or System.Text.Json.JsonException)
        {
            _logger.LogWarning(ex, "Failed to interpret Stripe webhook (event '{Event}')", eventType);
            return WebhookEventInterpretation.Ignored($"Unparseable Stripe event '{eventType}'.");
        }
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------
    private static WebhookEventInterpretation InterpretPaymentIntent(StripeEvent stripeEvent, PaymentStatus status)
    {
        if (stripeEvent.Data.Object is not PaymentIntent intent)
        {
            return WebhookEventInterpretation.Ignored("PaymentIntent payload missing.");
        }

        return new WebhookEventInterpretation
        {
            ProviderPaymentId = intent.Id,
            NewPaymentStatus = status,
            ErrorCode = intent.LastPaymentError?.Code,
            ErrorMessage = intent.LastPaymentError?.Message,
        };
    }

    private static WebhookEventInterpretation InterpretChargeRefunded(StripeEvent stripeEvent)
    {
        if (stripeEvent.Data.Object is not Charge charge)
        {
            return WebhookEventInterpretation.Ignored("Charge payload missing.");
        }

        return new WebhookEventInterpretation
        {
            ProviderPaymentId = charge.PaymentIntentId,
            NewPaymentStatus = charge.AmountRefunded >= charge.Amount ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded,
            RefundedAmountMinor = charge.AmountRefunded,
            NewRefundStatus = RefundStatus.Succeeded,
        };
    }

    private static PaymentStatus MapStatus(string stripeStatus) => stripeStatus switch
    {
        "succeeded" => PaymentStatus.Captured,
        "requires_capture" => PaymentStatus.Authorized,
        "canceled" => PaymentStatus.Cancelled,
        "requires_payment_method" or "requires_action" or "requires_confirmation" or "processing" => PaymentStatus.Pending,
        _ => PaymentStatus.Pending,
    };

    private static RefundStatus MapRefundStatus(string? raw) => raw switch
    {
        "succeeded" => RefundStatus.Succeeded,
        "failed" => RefundStatus.Failed,
        "canceled" => RefundStatus.Cancelled,
        "pending" or "requires_action" or null => RefundStatus.Pending,
        _ => RefundStatus.Pending,
    };

    private static string? MapReason(string? reason) => reason?.ToLowerInvariant() switch
    {
        "duplicate" => "duplicate",
        "fraudulent" => "fraudulent",
        "requested_by_customer" => "requested_by_customer",
        _ => null,
    };

    private static Dictionary<string, string> BuildMetadata(DomainPayment payment, CreatePaymentRequest request)
    {
        var metadata = request.Metadata is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>(request.Metadata);

        metadata["lm_payment_id"] = payment.Id.ToString();
        metadata["lm_customer_id"] = payment.CustomerId.ToString();
        if (payment.LicenseId.HasValue)
        {
            metadata["lm_license_id"] = payment.LicenseId.Value.ToString();
        }

        return metadata;
    }

    private void EnsureConfigured()
    {
        if (!_options.IsConfigured)
        {
            throw new InvalidOperationException(
                "Stripe is not configured. Populate Payments:Stripe (SecretKey/WebhookSecret).");
        }
    }
}
