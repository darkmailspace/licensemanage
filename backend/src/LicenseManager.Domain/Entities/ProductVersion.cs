using LicenseManager.Domain.Common;

namespace LicenseManager.Domain.Entities;

public class ProductVersion : BaseEntity
{
    public Guid ProductId { get; set; }
    public string Version { get; set; } = string.Empty;
    public string? ReleaseNotes { get; set; }
    public string? Changelog { get; set; }
    public DateTime ReleasedAt { get; set; }
    public bool IsStable { get; set; } = true;
    public bool IsBeta { get; set; } = false;
    public bool IsMajorUpdate { get; set; } = false;
    public bool IsForced { get; set; } = false;
    public string? DownloadUrl { get; set; }
    public long? FileSizeBytes { get; set; }
    public string? FileChecksum { get; set; }
    public string? MinimumCompatibleVersion { get; set; }
    
    // Navigation properties
    public virtual Product Product { get; set; } = null!;
    public virtual ICollection<UpdateDownload> Downloads { get; set; } = new List<UpdateDownload>();
}
