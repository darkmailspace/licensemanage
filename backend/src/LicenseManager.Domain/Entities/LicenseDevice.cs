using LicenseManager.Domain.Common;

namespace LicenseManager.Domain.Entities;

public class LicenseDevice : BaseEntity
{
    public Guid LicenseId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceFingerprint { get; set; } = string.Empty;
    
    // Hardware Identifiers
    public string? CPUId { get; set; }
    public string? MotherboardId { get; set; }
    public string? DiskSerialNumber { get; set; }
    public string? MacAddress { get; set; }
    public string? BIOSSerialNumber { get; set; }
    
    // System Information
    public string? OperatingSystem { get; set; }
    public string? OSVersion { get; set; }
    public string? Architecture { get; set; }
    public bool IsVirtualMachine { get; set; } = false;
    public string? VMPlatform { get; set; }
    
    // Network Information
    public string? IPAddress { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    
    // Device Status
    public bool IsActive { get; set; } = true;
    public DateTime? FirstActivatedAt { get; set; }
    public DateTime? LastAccessedAt { get; set; }
    public int AccessCount { get; set; } = 0;
    
    // Deactivation
    public bool IsDeactivated { get; set; } = false;
    public DateTime? DeactivatedAt { get; set; }
    public string? DeactivationReason { get; set; }
    
    // Navigation properties
    public virtual License License { get; set; } = null!;
}
