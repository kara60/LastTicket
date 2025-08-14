namespace TicketSystem.Domain.Events;

public class TicketClosedEvent : BaseEvent
{
    public int TicketId { get; }
    public string TicketNumber { get; }
    public int CustomerId { get; }
    public int ClosedByUserId { get; }
    public string? ClosingComment { get; }

    public TicketClosedEvent(int ticketId, string ticketNumber, int customerId, int closedByUserId, string? closingComment)
    {
        TicketId = ticketId;
        TicketNumber = ticketNumber;
        CustomerId = customerId;
        ClosedByUserId = closedByUserId;
        ClosingComment = closingComment;
    }
}