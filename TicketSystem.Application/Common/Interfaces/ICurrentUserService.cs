namespace TicketSystem.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? UserName { get; }
    Guid? CompanyId { get; }
    Guid? CustomerId { get; }
    string? Role { get; }
    bool IsAdmin { get; }
    bool IsCustomer { get; }
    bool IsAuthenticated { get; }
}