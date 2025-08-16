using System.Security.Claims;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Domain.Enums;

namespace TicketSystem.Web.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CurrentUserService> _logger;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, ILogger<CurrentUserService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public int? UserId
    {
        get
        {
            try
            {
                var context = _httpContextAccessor.HttpContext;
                if (context?.User == null)
                {
                    _logger.LogWarning("HttpContext or User is null in CurrentUserService");
                    return null;
                }

                var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    _logger.LogWarning("NameIdentifier claim not found. Available claims: {Claims}",
                        string.Join(", ", context.User.Claims.Select(c => $"{c.Type}:{c.Value}")));
                    return null;
                }

                if (int.TryParse(userIdClaim, out var id))
                {
                    _logger.LogDebug("UserId retrieved: {UserId}", id);
                    return id;
                }

                _logger.LogWarning("Failed to parse UserId: {UserIdClaim}", userIdClaim);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting UserId from CurrentUserService");
                return null;
            }
        }
    }

    public string? UserName
    {
        get
        {
            try
            {
                var userName = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name);
                _logger.LogDebug("UserName retrieved: {UserName}", userName);
                return userName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting UserName from CurrentUserService");
                return null;
            }
        }
    }

    public int? CompanyId
    {
        get
        {
            try
            {
                var companyIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirstValue("CompanyId");
                if (string.IsNullOrEmpty(companyIdClaim))
                {
                    _logger.LogWarning("CompanyId claim not found");
                    return null;
                }

                if (int.TryParse(companyIdClaim, out var id))
                {
                    _logger.LogDebug("CompanyId retrieved: {CompanyId}", id);
                    return id;
                }

                _logger.LogWarning("Failed to parse CompanyId: {CompanyIdClaim}", companyIdClaim);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting CompanyId from CurrentUserService");
                return null;
            }
        }
    }

    public int? CustomerId
    {
        get
        {
            try
            {
                var customerIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirstValue("CustomerId");
                if (string.IsNullOrEmpty(customerIdClaim))
                {
                    // Customer değil ise normal durum
                    return null;
                }

                if (int.TryParse(customerIdClaim, out var id))
                {
                    _logger.LogDebug("CustomerId retrieved: {CustomerId}", id);
                    return id;
                }

                _logger.LogWarning("Failed to parse CustomerId: {CustomerIdClaim}", customerIdClaim);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting CustomerId from CurrentUserService");
                return null;
            }
        }
    }

    public string? Role
    {
        get
        {
            try
            {
                var role = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role);
                _logger.LogDebug("Role retrieved: {Role}", role);
                return role;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Role from CurrentUserService");
                return null;
            }
        }
    }

    public bool IsAdmin => Role == UserRole.Admin.ToString();
    public bool IsCustomer => Role == UserRole.Customer.ToString();

    public bool IsAuthenticated
    {
        get
        {
            try
            {
                var isAuthenticated = _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
                _logger.LogDebug("IsAuthenticated: {IsAuthenticated}", isAuthenticated);
                return isAuthenticated;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking IsAuthenticated in CurrentUserService");
                return false;
            }
        }
    }

    // Debugging için ek method
    public void LogCurrentUserInfo()
    {
        try
        {
            var context = _httpContextAccessor.HttpContext;
            if (context?.User == null)
            {
                _logger.LogInformation("=== CURRENT USER DEBUG ===");
                _logger.LogInformation("HttpContext or User is NULL");
                _logger.LogInformation("========================");
                return;
            }

            _logger.LogInformation("=== CURRENT USER DEBUG ===");
            _logger.LogInformation("IsAuthenticated: {IsAuthenticated}", context.User.Identity?.IsAuthenticated);
            _logger.LogInformation("AuthenticationType: {AuthenticationType}", context.User.Identity?.AuthenticationType);
            _logger.LogInformation("UserId: {UserId}", UserId);
            _logger.LogInformation("UserName: {UserName}", UserName);
            _logger.LogInformation("Role: {Role}", Role);
            _logger.LogInformation("CompanyId: {CompanyId}", CompanyId);
            _logger.LogInformation("CustomerId: {CustomerId}", CustomerId);
            _logger.LogInformation("Claims Count: {ClaimsCount}", context.User.Claims.Count());

            foreach (var claim in context.User.Claims)
            {
                _logger.LogInformation("Claim: {Type} = {Value}", claim.Type, claim.Value);
            }
            _logger.LogInformation("========================");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in LogCurrentUserInfo");
        }
    }
}