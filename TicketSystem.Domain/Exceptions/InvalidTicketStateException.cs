using TicketSystem.Domain.Enums;

namespace TicketSystem.Domain.Exceptions;

public class InvalidTicketStateException : DomainException
{
    public TicketStatus CurrentStatus { get; }
    public string Operation { get; }

    public InvalidTicketStateException(TicketStatus currentStatus, string operation)
        : base($"Cannot perform operation '{operation}' on ticket with status '{currentStatus}'.")
    {
        CurrentStatus = currentStatus;
        Operation = operation;
    }
}