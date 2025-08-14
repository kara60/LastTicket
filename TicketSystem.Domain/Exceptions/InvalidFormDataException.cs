namespace TicketSystem.Domain.Exceptions;

public class InvalidFormDataException : DomainException
{
    public string FieldName { get; }

    public InvalidFormDataException(string fieldName, string message)
        : base($"Invalid form data for field '{fieldName}': {message}")
    {
        FieldName = fieldName;
    }
}