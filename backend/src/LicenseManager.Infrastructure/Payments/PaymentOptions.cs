namespace LicenseManager.Infrastructure.Payments;

/// <summary>
/// Root binding for the <c>Payments</c> section of appsettings. Each gateway
/// has its own subsection.
/// </summary>
public sealed class PaymentOptions
{
    public const string SectionName = "Payments";

    public RazorpayOptions Razorpay { get; set; } = new();
    public StripeOptions Stripe { get; set; } = new();
}

public sealed class RazorpayOptions
{
    public string KeyId { get; set; } = string.Empty;
    public string KeySecret { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.razorpay.com";

    /// <summary>Convenience flag - true when the three required secrets are populated.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(KeyId)
        && !string.IsNullOrWhiteSpace(KeySecret)
        && !string.IsNullOrWhiteSpace(WebhookSecret);
}

public sealed class StripeOptions
{
    public string SecretKey { get; set; } = string.Empty;
    public string PublishableKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(SecretKey)
        && !string.IsNullOrWhiteSpace(WebhookSecret);
}
