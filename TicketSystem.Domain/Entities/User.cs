using TicketSystem.Domain.Common;
using TicketSystem.Domain.Enums;
using TicketSystem.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Domain.Entities;

public class User : AuditableEntity
{
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public Email Email { get; set; } = null!;

    public PhoneNumber? Phone { get; set; }

    [Required, MaxLength(200)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? LastLoginAt { get; set; }

    [MaxLength(500)]
    public string? ProfileImageUrl { get; set; }

    // Foreign Keys
    public int CompanyId { get; set; }
    public int? CustomerId { get; set; } // Null for Admin users

    // Navigation Properties
    public virtual Company Company { get; set; } = null!;
    public virtual Customer? Customer { get; set; }
    public virtual ICollection<Ticket> CreatedTickets { get; set; } = new List<Ticket>();
    public virtual ICollection<Ticket> AssignedTickets { get; set; } = new List<Ticket>();
    public virtual ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
    public virtual ICollection<TicketHistory> TicketHistories { get; set; } = new List<TicketHistory>();


    // Computed Properties
    public string FullName => $"{FirstName} {LastName}";
    public bool IsAdmin => Role == UserRole.Admin;
    public bool IsCustomer => Role == UserRole.Customer;
}