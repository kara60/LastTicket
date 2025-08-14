using TicketSystem.Domain.Common;
using TicketSystem.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Domain.Entities;

public class TicketHistory : BaseEntity
{
    [Required, MaxLength(100)]
    public string Action { get; set; } = string.Empty; // Created, StatusChanged, Assigned, etc.

    [MaxLength(100)]
    public string? OldValue { get; set; }

    [MaxLength(100)]
    public string? NewValue { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public string? Details { get; set; } // JSON for complex changes

    // Foreign Keys
    public int TicketId { get; set; }
    public int UserId { get; set; }

    // Navigation Properties
    public virtual Ticket Ticket { get; set; } = null!;
    public virtual User User { get; set; } = null!;

    // Factory methods for common history entries
    public static TicketHistory CreateStatusChange(int ticketId, int userId, TicketStatus oldStatus, TicketStatus newStatus)
    {
        return new TicketHistory
        {
            TicketId = ticketId,
            UserId = userId,
            Action = "StatusChanged",
            OldValue = oldStatus.ToString(),
            NewValue = newStatus.ToString(),
            Description = GetStatusChangeDescription(oldStatus, newStatus),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId.ToString()
        };
    }

    private static string GetStatusChangeDescription(TicketStatus oldStatus, TicketStatus newStatus)
    {
        return newStatus switch
        {
            TicketStatus.İşlemde => "Ticket onaylandı ve işleme alındı",
            TicketStatus.Çözüldü => "Ticket çözüldü",
            TicketStatus.Kapandı => "Ticket kapatıldı",
            TicketStatus.Reddedildi => "Ticket reddedildi",
            _ => $"Durum değişti: {oldStatus} → {newStatus}"
        };
    }

    public static TicketHistory CreateAssignment(int ticketId, int userId, int? oldAssigneeId, int? newAssigneeId)
    {
        return new TicketHistory
        {
            TicketId = ticketId,
            UserId = userId,
            Action = "Assigned",
            OldValue = oldAssigneeId?.ToString(),
            NewValue = newAssigneeId?.ToString(),
            Description = newAssigneeId.HasValue ? "Ticket assigned" : "Ticket unassigned",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId.ToString()
        };
    }
}