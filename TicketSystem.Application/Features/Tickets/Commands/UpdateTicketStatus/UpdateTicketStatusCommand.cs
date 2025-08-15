using TicketSystem.Application.Common.Commands;
using TicketSystem.Domain.Enums;

namespace TicketSystem.Application.Features.Tickets.Commands.UpdateTicketStatus;

public class UpdateTicketStatusCommand : ICommand
{
    public int TicketId { get; set; }
    public TicketStatus NewStatus { get; set; }
    public string? Comment { get; set; }
    public bool SendToPmo { get; set; }
}