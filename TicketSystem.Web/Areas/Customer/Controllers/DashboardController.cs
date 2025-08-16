using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using TicketSystem.Application.Features.Dashboard.Queries.GetDashboardStats;
using TicketSystem.Application.Features.Tickets.Queries.GetMyTickets;

namespace TicketSystem.Web.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Roles = "Customer")]
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
            _logger.LogInformation("Customer dashboard requested by user: {UserId}",
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);

            // Dashboard istatistiklerini al
            var statsQuery = new GetDashboardStatsQuery();
            var statsResult = await _mediator.Send(statsQuery);

            // Son ticket'ları al (sadece 5 tane)
            var recentTicketsQuery = new GetMyTicketsQuery
            {
                Page = 1,
                PageSize = 5,
                SortBy = "CreatedAt",
                SortDescending = true
            };
            var recentTicketsResult = await _mediator.Send(recentTicketsQuery);

            if (statsResult.IsSuccess)
            {
                ViewData["Title"] = "Müşteri Dashboard";

                // Son ticket'ları ViewBag'e ekle
                if (recentTicketsResult.IsSuccess)
                {
                    ViewBag.RecentTickets = recentTicketsResult.Data?.Items;
                }

                return View(statsResult.Data);
            }
            else
            {
                _logger.LogWarning("Customer dashboard stats query failed: {Errors}",
                    string.Join(", ", statsResult.Errors));
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
            _logger.LogError(ex, "Error loading customer dashboard");
            TempData["Error"] = "Dashboard yüklenirken beklenmeyen bir hata oluştu.";

            // Empty model for error case
            var emptyData = new DashboardStatsDto();
            return View(emptyData);
        }
    }
}