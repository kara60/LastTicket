namespace TicketSystem.Domain.Events;

public class TicketRejectedEvent : BaseEvent
{
    public int TicketId { get; }
    public string TicketNumber { get; }
    public int CustomerId { get; }
    public int RejectedByUserId { get; }
    public string RejectionReason { get; }

    public TicketRejectedEvent(int ticketId, string ticketNumber, int customerId, int rejectedByUserId, string rejectionReason)
    {
        TicketId = ticketId;
        TicketNumber = ticketNumber;
        CustomerId = customerId;
        RejectedByUserId = rejectedByUserId;
        RejectionReason = rejectionReason;
    }
}