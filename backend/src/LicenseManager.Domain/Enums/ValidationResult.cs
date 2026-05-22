namespace LicenseManager.Domain.Enums;

public enum ValidationResult
{
    Valid = 1,
    Invalid = 2,
    Expired = 3,
    Revoked = 4,
    Suspended = 5,
    DomainMismatch = 6,
    HardwareMismatch = 7,
    MaxDevicesReached = 8,
    MaxUsersReached = 9,
    FeatureNotEnabled = 10,
    SignatureInvalid = 11,
    TamperedLicense = 12,
    IPRestricted = 13,
    CountryRestricted = 14,
    GracePeriodExpired = 15
}
