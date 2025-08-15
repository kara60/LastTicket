using TicketSystem.Application.Features.Common.DTOs;
using TicketSystem.Domain.Enums;

namespace TicketSystem.Application.Features.Tickets.DTOs;

public class TicketDto
{
    public Guid Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TicketStatus Status { get; set; }
    public string StatusDisplay { get; set; } = string.Empty;
    public Dictionary<string, object> FormData { get; set; } = new();
    public string? SelectedModule { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public TicketTypeDto Type { get; set; } = new();
    public TicketCategoryDto Category { get; set; } = new();
    public CustomerDto Customer { get; set; } = new();
    public UserDto CreatedBy { get; set; } = new();
    public UserDto? AssignedTo { get; set; }
    public List<TicketCommentDto> Comments { get; set; } = new();
    public List<TicketAttachmentDto> Attachments { get; set; } = new();
}