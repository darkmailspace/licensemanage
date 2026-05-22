using LicenseManager.Domain.Common;

namespace LicenseManager.Domain.Entities;

public enum TicketStatus
{
    Open = 1,
    InProgress = 2,
    Waiting = 3,
    Resolved = 4,
    Closed = 5,
    Cancelled = 6
}

public enum TicketPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public class SupportTicket : BaseEntity
{
    public string TicketNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Guid? LicenseId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    public string? AssignedTo { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? Resolution { get; set; }
    
    // Navigation properties
    public virtual Customer Customer { get; set; } = null!;
    public virtual License? License { get; set; }
    public virtual ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
}
