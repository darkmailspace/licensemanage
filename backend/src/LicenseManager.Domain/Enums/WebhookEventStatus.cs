namespace LicenseManager.Domain.Enums;

public enum WebhookEventStatus
{
    /// <summary>Body persisted, signature verified, not yet processed.</summary>
    Received = 0,

    /// <summary>Successfully applied to local state.</summary>
    Processed = 1,

    /// <summary>Processing threw and the event needs retry / triage.</summary>
    Failed = 2,

    /// <summary>Duplicate (by ProviderEventId) or unrecognised event type.</summary>
    Ignored = 3,
}
