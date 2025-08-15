using TicketSystem.Domain.Common;
using TicketSystem.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Domain.Entities;

public class Company : AuditableEntity
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    public Email Email { get; set; } = null!;

    public PhoneNumber? Phone { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [MaxLength(500)]
    public string? Website { get; set; }

    public bool IsActive { get; set; } = true;

    // System Settings
    public bool RequiresPMOIntegration { get; set; } = false;
    public bool AutoApproveTickets { get; set; } = false;
    public bool SendEmailNotifications { get; set; } = true;
    public bool AllowFileAttachments { get; set; } = true;
    public int MaxFileSize { get; set; } = 10;

    [MaxLength(1000)]
    public string? PMOApiEndpoint { get; set; }

    [MaxLength(500)]
    public string? PMOApiKey { get; set; }

    // Navigation
    public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();
    public virtual ICollection<User> Users { get; set; } = new List<User>();
    public virtual ICollection<TicketType> TicketTypes { get; set; } = new List<TicketType>();
    public virtual ICollection<TicketCategory> TicketCategories { get; set; } = new List<TicketCategory>();
    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}