namespace LicenseManager.Domain.Common;

public abstract class AuditableEntity : BaseEntity
{
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}
