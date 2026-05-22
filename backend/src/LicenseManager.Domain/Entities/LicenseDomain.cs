using LicenseManager.Domain.Common;

namespace LicenseManager.Domain.Entities;

public class LicenseDomain : BaseEntity
{
    public Guid LicenseId { get; set; }
    public string DomainName { get; set; } = string.Empty;
    public bool IsWildcard { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime? VerifiedAt { get; set; }
    public DateTime? LastAccessedAt { get; set; }
    public bool IsPrimary { get; set; } = false;
    public string? VerificationCode { get; set; }
    public bool IsVerified { get; set; } = false;
    
    // Transfer/Change Request
    public bool ChangeRequested { get; set; } = false;
    public string? RequestedDomain { get; set; }
    public DateTime? ChangeRequestedAt { get; set; }
    public bool ChangeApproved { get; set; } = false;
    public DateTime? ChangeApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }
    
    // Navigation properties
    public virtual License License { get; set; } = null!;
}
