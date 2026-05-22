using LicenseManager.Application.Common.Interfaces;
using LicenseManager.Application.Payments.Models;
using LicenseManager.Domain.Entities;
using LicenseManager.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LicenseManager.Infrastructure.Payments;

/// <summary>
/// Provider-agnostic orchestrator. Owns the persistence of <c>Payment</c>,
/// <c>Refund</c>, and <c>WebhookEvent</c> rows and delegates the
/// provider-specific work (HTTP / SDK calls, signature verification, JSON
/// parsing) to the registered <see cref="IPaymentGateway"/>.
/// </summary>
public sealed class PaymentService : IPaymentService
{
    private readonly IApplicationDbContext _db;
    private readonly IPaymentGatewayFactory _factory;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IApplicationDbContext db,
        IPaymentGatewayFactory factory,
        ILogger<PaymentService> logger)
    {
        _db = db;
        _factory = factory;
        _logger = logger;
    }

    // ---------------------------------------------------------------------
    // CREATE
    // ---------------------------------------------------------------------
    public async Task<PaymentSession> CreatePaymentAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than zero.", nameof(request));
        }

        var gateway = _factory.Get(request.Provider);

        var payment = new Payment
        {
            CustomerId = request.CustomerId,
            LicenseId = request.LicenseId,
            Provider = request.Provider,
            Amount = request.Amount,
            AmountMinor = MoneyUtil.ToMinor(request.Amount, request.Currency),
            Currency = request.Currency.ToUpperInvariant(),
            Description = request.Description,
            Receipt = request.Receipt,
            Metadata = request.Metadata,
            Status = PaymentStatus.Created,
        };

        _db.Payments.Add(payment);

        // Open the session at the provider; the gateway mutates the entity
        // in place with provider ids, status, raw response, etc.
        var session = await gateway.CreateSessionAsync(payment, request, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Payment {PaymentId} opened with {Provider} (amount={AmountMinor} {Currency}, status={Status})",
            payment.Id, payment.Provider, payment.AmountMinor, payment.Currency, payment.Status);

        return session;
    }

    // ---------------------------------------------------------------------
    // VERIFY
    // ---------------------------------------------------------------------
    public async Task<PaymentVerificationResult> VerifyPaymentAsync(
        PaymentVerificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken)
            ?? throw new InvalidOperationException($"Payment '{request.PaymentId}' not found.");

        var gateway = _factory.Get(payment.Provider);
        var result = await gateway.VerifyAsync(payment, request, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Payment {PaymentId} verified via {Provider}: success={Success}, status={Status}",
            payment.Id, payment.Provider, result.Success, result.Status);

        return result;
    }

    // ---------------------------------------------------------------------
    // REFUND
    // ---------------------------------------------------------------------
    public async Task<RefundResult> RefundPaymentAsync(
        RefundRequest request,
        CancellationToken cancellationToken = default)
    {
        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken)
            ?? throw new InvalidOperationException($"Payment '{request.PaymentId}' not found.");

        if (payment.Status is not (PaymentStatus.Captured or PaymentStatus.PartiallyRefunded))
        {
            return new RefundResult
            {
                Success = false,
                PaymentId = payment.Id,
                Status = RefundStatus.Failed,
                ErrorCode = "payment_not_refundable",
                ErrorMessage = $"Payment is in status '{payment.Status}'; only Captured or PartiallyRefunded payments can be refunded.",
            };
        }

        var refund = new Refund
        {
            PaymentId = payment.Id,
            Currency = payment.Currency,
            Reason = request.Reason,
            Status = RefundStatus.Pending,
        };
        _db.Refunds.Add(refund);

        var gateway = _factory.Get(payment.Provider);
        var result = await gateway.RefundAsync(payment, refund, request, cancellationToken);

        if (result.Success && refund.Status == RefundStatus.Succeeded)
        {
            payment.ApplyRefund(refund.AmountMinor, DateTime.UtcNow);
        }

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Refund {RefundId} for payment {PaymentId}: success={Success}, status={Status}, amount={AmountMinor} {Currency}",
            refund.Id, payment.Id, result.Success, result.Status, result.AmountMinor, result.Currency);

        return result with { RefundId = refund.Id };
    }

    // ---------------------------------------------------------------------
    // GET
    // ---------------------------------------------------------------------
    public Task<Payment?> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default)
        => _db.Payments
            .Include(p => p.Refunds)
            .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);

    // ---------------------------------------------------------------------
    // WEBHOOKS
    // ---------------------------------------------------------------------
    public async Task<WebhookProcessResult> HandleWebhookAsync(
        PaymentProvider provider,
        string rawBody,
        string? signatureHeader,
        CancellationToken cancellationToken = default)
    {
        var gateway = _factory.Get(provider);

        if (!gateway.VerifyWebhookSignature(rawBody, signatureHeader, out var eventId, out var eventType))
        {
            _logger.LogWarning(
                "Rejected {Provider} webhook (signature invalid; bytes={Length})",
                provider, rawBody.Length);
            return WebhookProcessResult.SignatureInvalid();
        }

        // Idempotency: bail out early if we have already processed this event.
        var existing = await _db.WebhookEvents
            .FirstOrDefaultAsync(
                e => e.Provider == provider && e.ProviderEventId == eventId,
                cancellationToken);

        if (existing is not null && existing.Status == WebhookEventStatus.Processed)
        {
            _logger.LogInformation(
                "{Provider} webhook {EventType} ({EventId}) already processed; ignoring duplicate",
                provider, eventType, eventId);
            return WebhookProcessResult.Duplicate(eventId, eventType);
        }

        var record = existing ?? new WebhookEvent
        {
            Provider = provider,
            ProviderEventId = eventId,
            EventType = eventType,
            Payload = rawBody,
            Signature = signatureHeader,
            ReceivedAt = DateTime.UtcNow,
            Status = WebhookEventStatus.Received,
        };

        if (existing is null)
        {
            _db.WebhookEvents.Add(record);
        }

        try
        {
            var interpretation = gateway.InterpretWebhook(rawBody, eventType);

            if (!interpretation.IsActionable)
            {
                record.Status = WebhookEventStatus.Ignored;
                record.ProcessedAt = DateTime.UtcNow;
                record.ErrorMessage = interpretation.Notes;
                await _db.SaveChangesAsync(cancellationToken);
                return WebhookProcessResult.Ignored(eventId, eventType, interpretation.Notes);
            }

            var payment = await ResolvePaymentAsync(provider, interpretation, cancellationToken);
            if (payment is null)
            {
                record.Status = WebhookEventStatus.Ignored;
                record.ProcessedAt = DateTime.UtcNow;
                record.ErrorMessage = "No matching local Payment row.";
                await _db.SaveChangesAsync(cancellationToken);
                return WebhookProcessResult.Ignored(eventId, eventType, "No matching local Payment row.");
            }

            ApplyInterpretation(payment, interpretation);

            record.PaymentId = payment.Id;
            record.Status = WebhookEventStatus.Processed;
            record.ProcessedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "{Provider} webhook {EventType} ({EventId}) applied to payment {PaymentId}",
                provider, eventType, eventId, payment.Id);

            return WebhookProcessResult.Processed(eventId, eventType, payment.Id);
        }
        catch (Exception ex)
        {
            record.Status = WebhookEventStatus.Failed;
            record.ProcessedAt = DateTime.UtcNow;
            record.ErrorMessage = ex.Message;

            // Best effort - swallow secondary exceptions while persisting the
            // failure so the original is what surfaces to the operator.
            try { await _db.SaveChangesAsync(cancellationToken); }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx,
                    "Failed to persist {Provider} webhook failure record (eventId={EventId})",
                    provider, eventId);
            }

            _logger.LogError(ex,
                "Unhandled exception while processing {Provider} webhook {EventType} ({EventId})",
                provider, eventType, eventId);

            return WebhookProcessResult.Error(ex.Message);
        }
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------
    private async Task<Payment?> ResolvePaymentAsync(
        PaymentProvider provider,
        WebhookEventInterpretation interpretation,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(interpretation.ProviderPaymentId))
        {
            var byPaymentId = await _db.Payments.FirstOrDefaultAsync(
                p => p.Provider == provider && p.ProviderPaymentId == interpretation.ProviderPaymentId,
                cancellationToken);

            if (byPaymentId is not null)
            {
                return byPaymentId;
            }
        }

        if (!string.IsNullOrEmpty(interpretation.ProviderOrderId))
        {
            var byOrderId = await _db.Payments.FirstOrDefaultAsync(
                p => p.Provider == provider && p.ProviderOrderId == interpretation.ProviderOrderId,
                cancellationToken);

            if (byOrderId is not null)
            {
                // First-time we see the payment id from the provider - record it.
                if (string.IsNullOrEmpty(byOrderId.ProviderPaymentId)
                    && !string.IsNullOrEmpty(interpretation.ProviderPaymentId))
                {
                    byOrderId.ProviderPaymentId = interpretation.ProviderPaymentId;
                }
                return byOrderId;
            }
        }

        return null;
    }

    private void ApplyInterpretation(Payment payment, WebhookEventInterpretation interp)
    {
        var now = DateTime.UtcNow;

        if (interp.NewPaymentStatus is { } status)
        {
            switch (status)
            {
                case PaymentStatus.Authorized: payment.MarkAuthorized(now); break;
                case PaymentStatus.Captured: payment.MarkCaptured(now); break;
                case PaymentStatus.Failed: payment.MarkFailed(now, interp.ErrorCode, interp.ErrorMessage); break;
                case PaymentStatus.Cancelled: payment.MarkCancelled(now); break;
                case PaymentStatus.Refunded:
                case PaymentStatus.PartiallyRefunded:
                    // Don't overwrite refunded/partial-refunded with a stale value;
                    // the RefundedAmountMinor below is authoritative.
                    payment.Status = status;
                    payment.UpdatedAt = now;
                    break;
                default:
                    payment.Status = status;
                    payment.UpdatedAt = now;
                    break;
            }
        }

        if (interp.RefundedAmountMinor is { } refundAmount && refundAmount > 0)
        {
            // Authoritative refunded amount comes from the provider event.
            // Recompute Payment.RefundedAmountMinor / Status accordingly.
            payment.RefundedAmountMinor = Math.Min(payment.AmountMinor, refundAmount);
            payment.Status = payment.RefundedAmountMinor >= payment.AmountMinor
                ? PaymentStatus.Refunded
                : PaymentStatus.PartiallyRefunded;
            payment.UpdatedAt = now;
        }

        if (!string.IsNullOrEmpty(interp.ProviderRefundId))
        {
            var refund = payment.Refunds.FirstOrDefault(r => r.ProviderRefundId == interp.ProviderRefundId);
            if (refund is null)
            {
                // Refund initiated provider-side without a local request (e.g.
                // dashboard refund). Backfill the row.
                refund = new Refund
                {
                    PaymentId = payment.Id,
                    ProviderRefundId = interp.ProviderRefundId,
                    Currency = payment.Currency,
                    Status = interp.NewRefundStatus ?? RefundStatus.Pending,
                };
                if (interp.RefundedAmountMinor.HasValue)
                {
                    refund.AmountMinor = interp.RefundedAmountMinor.Value;
                    refund.Amount = MoneyUtil.ToMajor(refund.AmountMinor, payment.Currency);
                }
                _db.Refunds.Add(refund);
            }
            else if (interp.NewRefundStatus is { } rs)
            {
                refund.Status = rs;
                refund.UpdatedAt = now;
                if (rs == RefundStatus.Succeeded) refund.RefundedAt = now;
            }
        }
    }
}
