using TicketSystem.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Domain.Entities;

public class TicketCategory : AuditableEntity
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? Icon { get; set; } = "folder";

    [MaxLength(7)]
    public string Color { get; set; } = "#10B981"; // Default green

    [MaxLength(200)]
    public string? CardTitle { get; set; }

    [MaxLength(500)]
    public string? CardDescription { get; set; }

    [MaxLength(200)]
    public string? CardSubtitle { get; set; }

    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    // Foreign Keys
    public int CompanyId { get; set; }

    // Navigation Properties
    public virtual Company Company { get; set; } = null!;
    public virtual ICollection<TicketCategoryModule> Modules { get; set; } = new List<TicketCategoryModule>();
    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}