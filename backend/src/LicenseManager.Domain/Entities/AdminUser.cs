using LicenseManager.Domain.Common;

namespace LicenseManager.Domain.Entities;

public enum UserRole
{
    SuperAdmin = 1,
    Admin = 2,
    Support = 3,
    Viewer = 4
}

public class AdminUser : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public UserRole Role { get; set; } = UserRole.Viewer;
    public bool IsActive { get; set; } = true;
    public bool EmailVerified { get; set; } = false;
    public DateTime? EmailVerifiedAt { get; set; }
    
    // MFA
    public bool MFAEnabled { get; set; } = false;
    public string? MFASecret { get; set; }
    public string? MFABackupCodes { get; set; } // JSON array
    
    // Security
    public DateTime? LastLoginAt { get; set; }
    public string? LastLoginIP { get; set; }
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockedUntil { get; set; }
    public string? IPWhitelist { get; set; } // Comma-separated
    
    // Password Reset
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiresAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<LoginHistory> LoginHistory { get; set; } = new List<LoginHistory>();
}
