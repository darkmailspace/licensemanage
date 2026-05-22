using LicenseManager.Domain.Common;

namespace LicenseManager.Domain.Entities;

public class ProductFeature : BaseEntity
{
    public Guid ProductId { get; set; }
    public Guid FeatureId { get; set; }
    public bool IsDefaultEnabled { get; set; } = false;
    public bool IsOptional { get; set; } = true;
    
    // Navigation properties
    public virtual Product Product { get; set; } = null!;
    public virtual Feature Feature { get; set; } = null!;
}
