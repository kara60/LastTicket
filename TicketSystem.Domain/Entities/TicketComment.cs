using TicketSystem.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Domain.Entities;

public class TicketComment : AuditableEntity
{
    [Required]
    public string Content { get; set; } = string.Empty;

    public bool IsInternal { get; set; } = false; // Only visible to admins
    public bool IsSystemGenerated { get; set; } = false; // Auto-generated comments

    // Foreign Keys
    public int TicketId { get; set; }
    public int UserId { get; set; }

    // Navigation Properties
    public virtual Ticket Ticket { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}