namespace TicketSystem.Domain.Events;

public class TicketApprovedEvent : BaseEvent
{
    public int TicketId { get; }
    public string TicketNumber { get; }
    public int CustomerId { get; }
    public int ApprovedByUserId { get; }
    public bool RequiresPMOIntegration { get; }
    public string? PMOApiEndpoint { get; }

    public TicketApprovedEvent(int ticketId, string ticketNumber, int customerId, int approvedByUserId, bool requiresPMOIntegration, string? pmoApiEndpoint = null)
    {
        TicketId = ticketId;
        TicketNumber = ticketNumber;
        CustomerId = customerId;
        ApprovedByUserId = approvedByUserId;
        RequiresPMOIntegration = requiresPMOIntegration;
        PMOApiEndpoint = pmoApiEndpoint;
    }
}