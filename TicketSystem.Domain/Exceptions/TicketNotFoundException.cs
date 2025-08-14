namespace TicketSystem.Domain.Exceptions;

public class TicketNotFoundException : DomainException
{
    public TicketNotFoundException(int ticketId)
        : base($"Ticket with ID {ticketId} was not found.") { }

    public TicketNotFoundException(string ticketNumber)
        : base($"Ticket with number {ticketNumber} was not found.") { }
}