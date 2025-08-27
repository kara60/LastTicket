using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using TicketSystem.Application.Features.Dashboard.Queries.GetDashboardStats;
using TicketSystem.Application.Features.Tickets.Queries.GetMyTickets;
using TicketSystem.Application.Common.Interfaces;

namespace TicketSystem.Web.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Roles = "Customer")]
public class DashboardController : Controller
{
    private readonly IMediator _mediator;
    private readonly ILogger<DashboardController> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public DashboardController(
        IMediator mediator,
        ILogger<DashboardController> logger,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _logger = logger;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
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

            // Son aktiviteleri al
            var recentActivities = await GetRecentActivitiesAsync();

            // Müşteri adını al
            string customerName = "Bilinmeyen Müşteri";
            if (_currentUserService.CustomerId.HasValue)
            {
                var customer = await _unitOfWork.Customers.GetByIdAsync(_currentUserService.CustomerId.Value);
                if (customer != null)
                {
                    customerName = customer.Name;
                }
            }

            if (statsResult.IsSuccess)
            {
                ViewData["Title"] = "Müşteri Dashboard";
                ViewBag.CustomerName = customerName;

                // Son ticket'ları ViewBag'e ekle
                if (recentTicketsResult.IsSuccess)
                {
                    ViewBag.RecentTickets = recentTicketsResult.Data?.Items;
                }

                // Son aktiviteleri ViewBag'e ekle
                ViewBag.RecentActivities = recentActivities;

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

                ViewBag.CustomerName = customerName;
                ViewBag.RecentActivities = recentActivities;

                return View(fallbackData);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading customer dashboard");
            TempData["Error"] = "Dashboard yüklenirken beklenmeyen bir hata oluştu.";

            // Empty model for error case
            var emptyData = new DashboardStatsDto();
            ViewBag.CustomerName = "Bilinmeyen Müşteri";
            ViewBag.RecentActivities = new List<RecentActivityDto>();

            return View(emptyData);
        }
    }

    private async Task<List<RecentActivityDto>> GetRecentActivitiesAsync()
    {
        try
        {
            // Son 15 günde bu müşteriye ait ticket aktiviteleri
            var activities = new List<RecentActivityDto>();
            var fifteenDaysAgo = DateTime.UtcNow.AddDays(-15);

            if (!_currentUserService.CustomerId.HasValue)
                return activities;

            // Son oluşturulan ticketlar
            var recentTickets = await _unitOfWork.Tickets.FindAsync(
                x => x.CustomerId == _currentUserService.CustomerId &&
                     x.CreatedAt >= fifteenDaysAgo,
                x => x.Type);

            foreach (var ticket in recentTickets.OrderByDescending(x => x.CreatedAt).Take(5))
            {
                activities.Add(new RecentActivityDto
                {
                    Type = "ticket_created",
                    Title = "Yeni ticket oluşturuldu",
                    Description = $"#{ticket.TicketNumber} - {ticket.Title}",
                    CreatedAt = ticket.CreatedAt,
                    Color = "bg-blue-500"
                });
            }

            // Son yorumlar
            var recentComments = await _unitOfWork.TicketComments.FindAsync(
                x => x.Ticket.CustomerId == _currentUserService.CustomerId &&
                     x.CreatedAt >= fifteenDaysAgo,
                x => x.Ticket);

            foreach (var comment in recentComments.OrderByDescending(x => x.CreatedAt).Take(5))
            {
                activities.Add(new RecentActivityDto
                {
                    Type = "comment_added",
                    Title = "Yorum eklendi",
                    Description = $"#{comment.Ticket.TicketNumber} - {(comment.Content.Length > 50 ? comment.Content.Substring(0, 50) + "..." : comment.Content)}",
                    CreatedAt = comment.CreatedAt,
                    Color = "bg-purple-500"
                });
            }

            // Durum değişiklikleri için ticket history (eğer var ise)
            // Bu kısım projenizde ticket history entity'si varsa eklenebilir

            return activities.OrderByDescending(x => x.CreatedAt).Take(10).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent activities");
            return new List<RecentActivityDto>();
        }
    }
}

// DTO for activities
public class RecentActivityDto
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Color { get; set; } = string.Empty;
}