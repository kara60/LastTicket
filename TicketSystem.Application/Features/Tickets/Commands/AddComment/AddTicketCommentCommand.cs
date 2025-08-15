using TicketSystem.Application.Common.Commands;

namespace TicketSystem.Application.Features.Tickets.Commands.AddComment;

public class AddTicketCommentCommand : ICommand<Guid>
{
    public Guid TicketId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsInternal { get; set; } = false;
}