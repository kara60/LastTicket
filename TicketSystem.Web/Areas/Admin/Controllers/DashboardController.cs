using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace TicketSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DashboardController : Controller
{
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(ILogger<DashboardController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        _logger.LogInformation("Admin Dashboard accessed by user: {User}", User.Identity.Name);
        _logger.LogInformation("User authenticated: {IsAuthenticated}", User.Identity.IsAuthenticated);
        _logger.LogInformation("User role: {Role}", User.FindFirst("role")?.Value);

        ViewBag.Message = "Admin Dashboard'a hoş geldiniz!";
        ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;
        ViewBag.UserRole = User.FindFirst("role")?.Value;
        ViewBag.UserName = User.Identity.Name;

        // Geçici istatistikler
        ViewBag.TotalTickets = 0;
        ViewBag.ActiveTickets = 0;
        ViewBag.ResolvedTickets = 0;
        ViewBag.ClosedTickets = 0;

        return View();
    }
}