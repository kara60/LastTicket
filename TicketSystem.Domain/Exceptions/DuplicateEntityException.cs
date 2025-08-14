namespace TicketSystem.Domain.Exceptions;

public class DuplicateEntityException : DomainException
{
    public string EntityType { get; }
    public string Field { get; }
    public string Value { get; }

    public DuplicateEntityException(string entityType, string field, string value)
        : base($"Duplicate {entityType} found with {field}: {value}")
    {
        EntityType = entityType;
        Field = field;
        Value = value;
    }
}