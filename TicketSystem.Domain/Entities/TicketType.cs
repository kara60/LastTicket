using TicketSystem.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Domain.Entities;

public class TicketType : AuditableEntity
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? Icon { get; set; } = "ticket";

    [MaxLength(7)]
    public string Color { get; set; } = "#3B82F6"; // Default blue

    [MaxLength(200)]
    public string? CardTitle { get; set; }

    [MaxLength(500)]
    public string? CardDescription { get; set; }

    [MaxLength(200)]
    public string? CardSubtitle { get; set; }

    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    // Form Definition (JSON)
    public string? FormDefinition { get; set; }

    // Foreign Key
    public int CompanyId { get; set; }

    // Navigation Properties
    public virtual Company Company { get; set; } = null!;
    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}