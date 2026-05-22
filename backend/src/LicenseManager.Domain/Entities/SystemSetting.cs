using LicenseManager.Domain.Common;

namespace LicenseManager.Domain.Entities;

/// <summary>
/// Key-value system settings persisted in the database.
/// Used for SMTP, SMS, WhatsApp, security, and feature flags.
/// </summary>
public class SystemSetting : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }
    public bool IsSecret { get; set; } = false;
}
