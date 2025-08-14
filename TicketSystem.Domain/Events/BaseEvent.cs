namespace TicketSystem.Domain.Events;

public abstract class BaseEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid Id { get; } = Guid.NewGuid();
}