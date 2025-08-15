namespace TicketSystem.Application.Features.Tickets.DTOs;

public class TicketListDto
{
    public Guid Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string StatusDisplay { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public string TypeColor { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public string? AssignedToName { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CommentCount { get; set; }
    public bool HasAttachments { get; set; }
}