namespace TicketSystem.Domain.Events;

public class TicketCreatedEvent : BaseEvent
{
    public int TicketId { get; }
    public int CustomerId { get; }
    public int CreatedByUserId { get; }
    public string TicketTitle { get; }
    public string TicketNumber { get; }

    public TicketCreatedEvent(int ticketId, int customerId, int createdByUserId, string ticketTitle, string ticketNumber)
    {
        TicketId = ticketId;
        CustomerId = customerId;
        CreatedByUserId = createdByUserId;
        TicketTitle = ticketTitle;
        TicketNumber = ticketNumber;
    }
}