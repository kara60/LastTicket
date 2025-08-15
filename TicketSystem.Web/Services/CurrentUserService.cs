using System.Security.Claims;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Domain.Enums;

namespace TicketSystem.Web.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? UserId
    {
        get
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userId, out var id) ? id : null;
        }
    }

    public string? UserName => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name);

    public int? CompanyId
    {
        get
        {
            var companyId = _httpContextAccessor.HttpContext?.User?.FindFirstValue("CompanyId");
            return int.TryParse(companyId, out var id) ? id : null;
        }
    }

    public int? CustomerId
    {
        get
        {
            var customerId = _httpContextAccessor.HttpContext?.User?.FindFirstValue("CustomerId");
            return int.TryParse(customerId, out var id) ? id : null;
        }
    }

    public string? Role => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role);

    public bool IsAdmin => Role == UserRole.Admin.ToString();
    public bool IsCustomer => Role == UserRole.Customer.ToString();
    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}