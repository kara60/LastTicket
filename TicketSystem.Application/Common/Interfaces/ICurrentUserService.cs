namespace TicketSystem.Application.Common.Interfaces;

public interface ICurrentUserService
{
    int? UserId { get; }
    string? UserName { get; }
    int? CompanyId { get; }
    int? CustomerId { get; }
    string? Role { get; }
    bool IsAdmin { get; }
    bool IsCustomer { get; }
    bool IsAuthenticated { get; }
}