namespace LicenseManager.Domain.Enums;

public enum LicenseStatus
{
    PendingActivation = 1,
    Active = 2,
    Suspended = 3,
    Expired = 4,
    Revoked = 5,
    GracePeriod = 6,
    PendingRenewal = 7,
    Cancelled = 8,
    Transferred = 9,
    Upgraded = 10,
    Downgraded = 11
}
