using TicketSystem.Domain.Enums;

namespace TicketSystem.Domain.Events;

public class TicketStatusChangedEvent : BaseEvent
{
    public int TicketId { get; }
    public string TicketNumber { get; }
    public TicketStatus OldStatus { get; }
    public TicketStatus NewStatus { get; }
    public int ChangedByUserId { get; }
    public int CustomerId { get; }

    public TicketStatusChangedEvent(int ticketId, string ticketNumber, TicketStatus oldStatus, TicketStatus newStatus, int changedByUserId, int customerId)
    {
        TicketId = ticketId;
        TicketNumber = ticketNumber;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        ChangedByUserId = changedByUserId;
        CustomerId = customerId;
    }
}