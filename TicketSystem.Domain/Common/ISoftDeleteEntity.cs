namespace TicketSystem.Domain.Common;

public interface ISoftDeleteEntity
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    string? DeletedBy { get; set; }
}