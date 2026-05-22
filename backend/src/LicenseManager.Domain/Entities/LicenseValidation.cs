using LicenseManager.Domain.Common;
using LicenseManager.Domain.Enums;

namespace LicenseManager.Domain.Entities;

public class LicenseValidation : BaseEntity
{
    public Guid LicenseId { get; set; }
    public ValidationResult ValidationResult { get; set; }
    public bool IsValid { get; set; }
    public string? ValidationMessage { get; set; }
    
    // Request Information
    public string? DomainName { get; set; }
    public string? DeviceFingerprint { get; set; }
    public string? IPAddress { get; set; }
    public string? Country { get; set; }
    public string? UserAgent { get; set; }
    public string? ProductVersion { get; set; }
    
    // Feature Validation
    public string? RequestedFeatures { get; set; } // JSON array of feature codes
    public string? EnabledFeatures { get; set; } // JSON array of enabled feature codes
    
    // Heartbeat Information
    public bool IsHeartbeat { get; set; } = false;
    public DateTime? LastHeartbeatAt { get; set; }
    
    // Response Time
    public int ResponseTimeMs { get; set; }
    
    // Navigation properties
    public virtual License License { get; set; } = null!;
}
