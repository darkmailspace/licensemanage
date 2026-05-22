using LicenseManager.Domain.Common;

namespace LicenseManager.Domain.Entities;

public class Product : BaseEntity
{
    public string ProductCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Version { get; set; } = "1.0.0";
    public bool IsActive { get; set; } = true;
    public string? ImageUrl { get; set; }
    public decimal BasePrice { get; set; }
    public string? Currency { get; set; } = "USD";
    public int TrialDays { get; set; } = 14;
    public bool AllowTrial { get; set; } = true;
    public int MaxDevicesPerLicense { get; set; } = 1;
    public int MaxUsersPerLicense { get; set; } = 1;
    public int MaxBranchesPerLicense { get; set; } = 1;
    public bool RequireDomainLock { get; set; } = true;
    public bool RequireHardwareLock { get; set; } = false;
    public int GracePeriodDays { get; set; } = 7;
    public int ValidationIntervalHours { get; set; } = 24;
    
    // Navigation properties
    public virtual ICollection<License> Licenses { get; set; } = new List<License>();
    public virtual ICollection<ProductFeature> ProductFeatures { get; set; } = new List<ProductFeature>();
    public virtual ICollection<ProductVersion> Versions { get; set; } = new List<ProductVersion>();
}
