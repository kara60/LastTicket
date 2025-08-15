using TicketSystem.Application.Features.Common.DTOs;

namespace TicketSystem.Application.Features.Tickets.DTOs;

public class TicketAttachmentDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime CreatedAt { get; set; }
    public UserDto UploadedBy { get; set; } = new();
}