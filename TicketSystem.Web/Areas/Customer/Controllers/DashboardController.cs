using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Application.Features.Dashboard.Queries.GetDashboardStats;
using TicketSystem.Application.Features.Tickets.Queries.GetMyTickets;

namespace TicketSystem.Web.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Policy = "CustomerOnly")]
public class DashboardController : Controller
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> Index()
    {
        // Son 5 ticket'ı al
        var recentTickets = await _mediator.Send(new GetMyTicketsQuery { PageSize = 5 });

        // Dashboard stats (customer için filtered)
        var stats = await _mediator.Send(new GetDashboardStatsQuery());

        ViewBag.RecentTickets = recentTickets.Data?.Items;
        return View(stats.Data);
    }
}