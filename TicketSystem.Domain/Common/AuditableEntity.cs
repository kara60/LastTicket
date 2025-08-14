namespace TicketSystem.Domain.Common;

public abstract class AuditableEntity : BaseEntity, IAuditableEntity, ISoftDeleteEntity
{
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}