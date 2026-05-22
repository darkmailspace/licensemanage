using LicenseManager.Domain.Common;
using LicenseManager.Domain.Enums;

namespace LicenseManager.Domain.Entities;

public class License : BaseEntity
{
    public string LicenseKey { get; set; } = string.Empty;
    public string ActivationToken { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Guid ProductId { get; set; }
    public LicenseType LicenseType { get; set; }
    public LicenseStatus Status { get; set; } = LicenseStatus.PendingActivation;
    
    // Limits
    public int MaxUsers { get; set; } = 1;
    public int MaxBranches { get; set; } = 1;
    public int MaxDomains { get; set; } = 1;
    public int MaxDevices { get; set; } = 1;
    public int MaxConcurrentLogins { get; set; } = 1;
    public long MaxApiCalls { get; set; } = 10000;
    public long MaxStorageGB { get; set; } = 10;
    public int MaxEmployees { get; set; } = 100;
    public int MaxCustomers { get; set; } = 1000;
    public int MaxLoans { get; set; } = 10000;
    public int MaxCollections { get; set; } = 10000;
    
    // Dates
    public DateTime StartDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? LastValidatedAt { get; set; }
    public DateTime? SuspendedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    
    // Security
    public string? LicenseSignature { get; set; }
    public string? PublicKey { get; set; }
    public string? EncryptedPayload { get; set; }
    public string? HardwareFingerprint { get; set; }
    
    // Locking
    public bool DomainLockEnabled { get; set; } = true;
    public bool HardwareLockEnabled { get; set; } = false;
    public bool IPLockEnabled { get; set; } = false;
    public bool CountryLockEnabled { get; set; } = false;
    public string? AllowedCountries { get; set; } // Comma-separated
    public string? IPWhitelist { get; set; } // Comma-separated
    
    // Grace Period
    public bool InGracePeriod { get; set; } = false;
    public DateTime? GracePeriodStartDate { get; set; }
    public int GracePeriodDays { get; set; } = 7;
    
    // Billing
    public decimal Price { get; set; }
    public string? Currency { get; set; } = "USD";
    public bool AutoRenewal { get; set; } = false;
    public string? PaymentMethod { get; set; }
    
    // Additional Info
    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }
    public Dictionary<string, object>? CustomMetadata { get; set; }
    
    // Navigation properties
    public virtual Customer Customer { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
    public virtual ICollection<LicenseDomain> Domains { get; set; } = new List<LicenseDomain>();
    public virtual ICollection<LicenseDevice> Devices { get; set; } = new List<LicenseDevice>();
    public virtual ICollection<LicenseActivation> Activations { get; set; } = new List<LicenseActivation>();
    public virtual ICollection<LicenseValidation> Validations { get; set; } = new List<LicenseValidation>();
    public virtual ICollection<LicenseFeatureMapping> FeatureMappings { get; set; } = new List<LicenseFeatureMapping>();
    public virtual ICollection<LicenseHistory> History { get; set; } = new List<LicenseHistory>();
    
    // Business Logic Methods
    public bool IsExpired() => DateTime.UtcNow > ExpiryDate;
    
    public bool IsActive() => Status == LicenseStatus.Active && !IsExpired();
    
    public bool CanActivate() => Status == LicenseStatus.PendingActivation && !IsExpired();
    
    public int DaysUntilExpiry() => (ExpiryDate - DateTime.UtcNow).Days;
    
    public bool IsInGracePeriod()
    {
        if (!InGracePeriod || GracePeriodStartDate == null) return false;
        return DateTime.UtcNow <= GracePeriodStartDate.Value.AddDays(GracePeriodDays);
    }
    
    public void Activate()
    {
        Status = LicenseStatus.Active;
        ActivatedAt = DateTime.UtcNow;
        LastValidatedAt = DateTime.UtcNow;
        InGracePeriod = false;
        GracePeriodStartDate = null;
    }
    
    public void Suspend(string? reason = null)
    {
        Status = LicenseStatus.Suspended;
        SuspendedAt = DateTime.UtcNow;
        if (reason != null)
        {
            InternalNotes = $"{InternalNotes}\n[{DateTime.UtcNow}] Suspended: {reason}";
        }
    }
    
    public void Revoke(string? reason = null)
    {
        Status = LicenseStatus.Revoked;
        RevokedAt = DateTime.UtcNow;
        if (reason != null)
        {
            InternalNotes = $"{InternalNotes}\n[{DateTime.UtcNow}] Revoked: {reason}";
        }
    }
    
    public void StartGracePeriod()
    {
        InGracePeriod = true;
        GracePeriodStartDate = DateTime.UtcNow;
        Status = LicenseStatus.GracePeriod;
    }
}
