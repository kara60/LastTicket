using TicketSystem.Domain.Common;
using TicketSystem.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Domain.Entities;

public class Customer : AuditableEntity
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    public Email ContactEmail { get; set; } = null!;

    public PhoneNumber? ContactPhone { get; set; }

    [MaxLength(100)]
    public string? ContactPerson { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    public bool IsActive { get; set; } = true;

    // Foreign Key
    public int CompanyId { get; set; }

    // Navigation Properties
    public virtual Company Company { get; set; } = null!;
    public virtual ICollection<User> Users { get; set; } = new List<User>();
    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}