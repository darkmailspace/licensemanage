using LicenseManager.Domain.Common;
using LicenseManager.Domain.Enums;

namespace LicenseManager.Domain.Entities;

public class LicenseHistory : BaseEntity
{
    public Guid LicenseId { get; set; }
    public string Action { get; set; } = string.Empty;
    public LicenseStatus? PreviousStatus { get; set; }
    public LicenseStatus? NewStatus { get; set; }
    public string? Description { get; set; }
    public string? PerformedBy { get; set; }
    public string? IPAddress { get; set; }
    public Dictionary<string, object>? Changes { get; set; }
    
    // Navigation properties
    public virtual License License { get; set; } = null!;
}
