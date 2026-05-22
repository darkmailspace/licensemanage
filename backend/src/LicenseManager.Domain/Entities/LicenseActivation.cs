using LicenseManager.Domain.Common;
using LicenseManager.Domain.Enums;

namespace LicenseManager.Domain.Entities;

public class LicenseActivation : BaseEntity
{
    public Guid LicenseId { get; set; }
    public ActivationType ActivationType { get; set; }
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    
    // Request Information
    public string? DomainName { get; set; }
    public string? DeviceFingerprint { get; set; }
    public string? IPAddress { get; set; }
    public string? Country { get; set; }
    public string? UserAgent { get; set; }
    
    // Offline Activation
    public string? ActivationRequestFile { get; set; }
    public string? ActivationResponseFile { get; set; }
    public DateTime? OfflineActivationGeneratedAt { get; set; }
    
    // Validation
    public string? ActivationCode { get; set; }
    public DateTime? ActivationCodeExpiresAt { get; set; }
    
    // Metadata
    public Dictionary<string, object>? RequestMetadata { get; set; }
    public Dictionary<string, object>? ResponseMetadata { get; set; }
    
    // Navigation properties
    public virtual License License { get; set; } = null!;
}
