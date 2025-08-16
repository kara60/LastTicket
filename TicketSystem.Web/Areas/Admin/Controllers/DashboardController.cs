using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using TicketSystem.Application.Features.Dashboard.Queries.GetDashboardStats;

namespace TicketSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class DashboardController : Controller
{
    private readonly IMediator _mediator;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IMediator mediator, ILogger<DashboardController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            _logger.LogInformation("Admin dashboard requested by user: {UserId}",
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);

            // Dashboard istatistiklerini al
            var query = new GetDashboardStatsQuery();
            var result = await _mediator.Send(query);

            if (result.IsSuccess)
            {
                ViewData["Title"] = "Admin Dashboard";
                return View(result.Data);
            }
            else
            {
                _logger.LogWarning("Dashboard stats query failed: {Errors}", string.Join(", ", result.Errors));
                TempData["Error"] = "Dashboard verileri yüklenirken bir hata oluştu.";

                // Fallback data
                var fallbackData = new DashboardStatsDto
                {
                    TotalTickets = 0,
                    ActiveTickets = 0,
                    ResolvedTickets = 0,
                    ClosedTickets = 0,
                    TicketsByStatus = new List<TicketsByStatusDto>(),
                    TicketsByType = new List<TicketsByTypeDto>(),
                    TicketsByCustomer = new List<TicketsByCustomerDto>(),
                    TicketsTrend = new List<TicketsTrendDto>()
                };

                return View(fallbackData);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading admin dashboard");
            TempData["Error"] = "Dashboard yüklenirken beklenmeyen bir hata oluştu.";

            // Empty model for error case
            var emptyData = new DashboardStatsDto();
            return View(emptyData);
        }
    }
}