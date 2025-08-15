using TicketSystem.Application.Features.Common.DTOs;

namespace TicketSystem.Application.Features.Tickets.DTOs;

public class TicketCommentDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    public DateTime CreatedAt { get; set; }
    public UserDto User { get; set; } = new();
}