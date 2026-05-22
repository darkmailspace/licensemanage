using LicenseManager.Domain.Common;

namespace LicenseManager.Domain.Entities;

public class UpdateDownload : BaseEntity
{
    public Guid ProductVersionId { get; set; }
    public Guid LicenseId { get; set; }
    public string? IPAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime DownloadedAt { get; set; } = DateTime.UtcNow;
    public bool IsCompleted { get; set; } = false;
    public DateTime? CompletedAt { get; set; }
    public long? BytesDownloaded { get; set; }
    
    // Navigation properties
    public virtual ProductVersion ProductVersion { get; set; } = null!;
    public virtual License License { get; set; } = null!;
}
