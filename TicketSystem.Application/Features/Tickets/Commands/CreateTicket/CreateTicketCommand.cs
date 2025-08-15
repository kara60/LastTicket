using TicketSystem.Application.Common.Commands;

namespace TicketSystem.Application.Features.Tickets.Commands.CreateTicket;

public class CreateTicketCommand : ICommand<string>
{
    public Guid TypeId { get; set; }
    public Guid CategoryId { get; set; }
    public string? SelectedModule { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Dictionary<string, object> FormData { get; set; } = new();
}