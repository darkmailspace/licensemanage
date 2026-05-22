using LicenseManager.Domain.Common;

namespace LicenseManager.Domain.Entities;

public class LoginHistory : BaseEntity
{
    public Guid UserId { get; set; }
    public bool Success { get; set; }
    public string? IPAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? FailureReason { get; set; }
    public DateTime LoginAttemptAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual AdminUser User { get; set; } = null!;
}
