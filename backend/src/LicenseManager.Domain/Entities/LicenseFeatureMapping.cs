using LicenseManager.Domain.Common;

namespace LicenseManager.Domain.Entities;

public class LicenseFeatureMapping : BaseEntity
{
    public Guid LicenseId { get; set; }
    public Guid FeatureId { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime? EnabledAt { get; set; }
    public DateTime? DisabledAt { get; set; }
    public string? DisabledBy { get; set; }
    public string? DisabledReason { get; set; }
    
    // Usage Limits
    public int? UsageLimit { get; set; }
    public int? UsageCount { get; set; } = 0;
    public DateTime? UsageLimitResetDate { get; set; }
    
    // Navigation properties
    public virtual License License { get; set; } = null!;
    public virtual Feature Feature { get; set; } = null!;
}
