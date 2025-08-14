namespace TicketSystem.Domain.Events;

public class CommentAddedEvent : BaseEvent
{
    public int TicketId { get; }
    public string TicketNumber { get; }
    public int CommentId { get; }
    public int UserId { get; }
    public int CustomerId { get; }
    public string CommentContent { get; }
    public bool IsInternal { get; }

    public CommentAddedEvent(int ticketId, string ticketNumber, int commentId, int userId, int customerId, string commentContent, bool isInternal)
    {
        TicketId = ticketId;
        TicketNumber = ticketNumber;
        CommentId = commentId;
        UserId = userId;
        CustomerId = customerId;
        CommentContent = commentContent;
        IsInternal = isInternal;
    }
}