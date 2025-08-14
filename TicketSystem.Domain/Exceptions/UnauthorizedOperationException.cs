namespace TicketSystem.Domain.Exceptions;

public class UnauthorizedOperationException : DomainException
{
    public int UserId { get; }
    public string Operation { get; }

    public UnauthorizedOperationException(int userId, string operation)
        : base($"User {userId} is not authorized to perform operation: {operation}")
    {
        UserId = userId;
        Operation = operation;
    }
}