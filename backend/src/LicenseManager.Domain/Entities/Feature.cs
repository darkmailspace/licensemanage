using LicenseManager.Domain.Common;

namespace LicenseManager.Domain.Entities;

public class Feature : BaseEntity
{
    public string FeatureCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public bool IsActive { get; set; } = true;
    public bool RequiresEnterpriseLicense { get; set; } = false;
    public decimal? AdditionalCost { get; set; }
    public int DisplayOrder { get; set; } = 0;
    
    // Navigation properties
    public virtual ICollection<LicenseFeatureMapping> LicenseFeatureMappings { get; set; } = new List<LicenseFeatureMapping>();
    public virtual ICollection<ProductFeature> ProductFeatures { get; set; } = new List<ProductFeature>();
}
