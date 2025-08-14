using TicketSystem.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Domain.Entities;

public class TicketCategoryModule : AuditableEntity
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? Icon { get; set; }

    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    // Foreign Key
    public int TicketCategoryId { get; set; }

    // Navigation Properties
    public virtual TicketCategory TicketCategory { get; set; } = null!;
    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}