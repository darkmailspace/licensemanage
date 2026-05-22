using LicenseManager.Domain.Common;

namespace LicenseManager.Domain.Entities;

public class TicketComment : BaseEntity
{
    public Guid TicketId { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string? CommentedBy { get; set; }
    public bool IsInternal { get; set; } = false;
    public string? Attachments { get; set; } // JSON array of file URLs
    
    // Navigation properties
    public virtual SupportTicket Ticket { get; set; } = null!;
}
