using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using TicketSystem.Application.Features.Dashboard.Queries.GetDashboardStats;
using TicketSystem.Application.Features.Tickets.Queries.GetMyTickets;
using TicketSystem.Application.Common.Interfaces;

namespace TicketSystem.Web.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Policy = "CustomerOnly")]
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

            // Son ticket'ları al (10 tane)
            var recentTicketsQuery = new GetMyTicketsQuery
            {
                Page = 1,
                PageSize = 10,
                SortBy = "CreatedAt",
                SortDescending = true
            };
            var recentTicketsResult = await _mediator.Send(recentTicketsQuery);

            // Son aktiviteleri al
            var recentActivities = await GetRecentActivitiesAsync();

            // Müşteri bilgilerini al
            var customerInfo = await GetCustomerInfoAsync();

            if (statsResult.IsSuccess)
            {
                ViewData["Title"] = "Dashboard";
                ViewBag.CustomerName = customerInfo.Name;
                ViewBag.CustomerInfo = customerInfo;

                // Son ticket'ları ViewBag'e ekle
                if (recentTicketsResult.IsSuccess)
                {
                    ViewBag.RecentTickets = recentTicketsResult.Data?.Items;
                }

                // Son aktiviteleri ViewBag'e ekle
                ViewBag.RecentActivities = recentActivities;

                // Dashboard verilerini enhance et
                var enhancedStats = await EnhanceDashboardStats(statsResult.Data);

                return View(enhancedStats);
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
                    TicketsByCategory = new List<TicketsByCategoryDto>(),
                    TicketsByCustomer = new List<TicketsByCustomerDto>(),
                    TicketsTrend = new List<TicketsTrendDto>()
                };

                ViewBag.CustomerName = customerInfo.Name;
                ViewBag.CustomerInfo = customerInfo;
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
            var customerInfo = await GetCustomerInfoAsync();
            ViewBag.CustomerName = customerInfo.Name;
            ViewBag.CustomerInfo = customerInfo;
            ViewBag.RecentActivities = new List<RecentActivityDto>();

            return View(emptyData);
        }
    }

    private async Task<CustomerInfoDto> GetCustomerInfoAsync()
    {
        try
        {
            if (_currentUserService.CustomerId.HasValue)
            {
                var customer = await _unitOfWork.Customers.GetByIdAsync(_currentUserService.CustomerId.Value);
                if (customer != null)
                {
                    return new CustomerInfoDto
                    {
                        Id = customer.Id,
                        Name = customer.Name,
                        Email = customer.ContactEmail?.Value ?? "Belirtilmemiş", // Customer entity'sinde ContactEmail var
                        Phone = customer.ContactPhone?.Value ?? "Belirtilmemiş", // Customer entity'sinde ContactPhone var
                        CreatedAt = customer.CreatedAt,
                        IsActive = customer.IsActive
                    };
                }
            }

            return new CustomerInfoDto
            {
                Name = "Bilinmeyen Müşteri",
                Email = "Belirtilmemiş",
                Phone = "Belirtilmemiş",
                CreatedAt = DateTime.Now,
                IsActive = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading customer info");
            return new CustomerInfoDto { Name = "Bilinmeyen Müşteri" };
        }
    }

    private async Task<DashboardStatsDto> EnhanceDashboardStats(DashboardStatsDto originalStats)
    {
        try
        {
            if (!_currentUserService.CustomerId.HasValue)
                return originalStats;

            // Reddedildi durumundaki ticket'ları kontrol et ve ekle
            var rejectedTicketsCount = await _unitOfWork.Tickets.CountAsync(
                x => x.CustomerId == _currentUserService.CustomerId &&
                     x.Status == Domain.Enums.TicketStatus.Reddedildi); // Türkçe enum değeri

            // Eğer reddedildi durumu yoksa ekle
            var rejectedStatus = originalStats.TicketsByStatus.FirstOrDefault(x => x.Status == "Reddedildi");
            if (rejectedStatus == null && rejectedTicketsCount > 0)
            {
                var rejectedStatusDto = new TicketsByStatusDto
                {
                    Status = "Reddedildi",
                    Count = rejectedTicketsCount,
                    Color = "#EF4444" // Red color
                };
                originalStats.TicketsByStatus.Add(rejectedStatusDto);
            }

            return originalStats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enhancing dashboard stats");
            return originalStats;
        }
    }

    private async Task<List<RecentActivityDto>> GetRecentActivitiesAsync()
    {
        try
        {
            // Son 30 günde bu müşteriye ait ticket aktiviteleri
            var activities = new List<RecentActivityDto>();
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

            if (!_currentUserService.CustomerId.HasValue)
                return activities;

            // Son oluşturulan ticketlar (son 10)
            var recentTickets = await _unitOfWork.Tickets.FindAsync(
                x => x.CustomerId == _currentUserService.CustomerId &&
                     x.CreatedAt >= thirtyDaysAgo,
                x => x.Type);

            foreach (var ticket in recentTickets.OrderByDescending(x => x.CreatedAt).Take(10))
            {
                activities.Add(new RecentActivityDto
                {
                    Type = "ticket_created",
                    Title = "Yeni ticket oluşturuldu",
                    Description = $"#{ticket.TicketNumber} - {(ticket.Title.Length > 40 ? ticket.Title.Substring(0, 40) + "..." : ticket.Title)}",
                    CreatedAt = ticket.CreatedAt,
                    Color = "bg-blue-500"
                });
            }

            // Son yorumlar (son 15)
            var recentComments = await _unitOfWork.TicketComments.FindAsync(
                x => x.Ticket.CustomerId == _currentUserService.CustomerId &&
                     x.CreatedAt >= thirtyDaysAgo,
                x => x.Ticket,
                x => x.User);

            foreach (var comment in recentComments.OrderByDescending(x => x.CreatedAt).Take(15))
            {
                activities.Add(new RecentActivityDto
                {
                    Type = "comment_added",
                    Title = comment.IsInternal ? "Dahili not eklendi" : "Yorum eklendi",
                    Description = $"#{comment.Ticket.TicketNumber} - {(comment.Content.Length > 50 ? comment.Content.Substring(0, 50) + "..." : comment.Content)}",
                    CreatedAt = comment.CreatedAt,
                    Color = comment.IsInternal ? "bg-orange-500" : "bg-purple-500"
                });
            }

            // Durum değişiklikleri için ticket'ları analiz et
            var statusChangedTickets = await _unitOfWork.Tickets.FindAsync(
                x => x.CustomerId == _currentUserService.CustomerId &&
                     x.UpdatedAt >= thirtyDaysAgo &&
                     x.UpdatedAt > x.CreatedAt.AddMinutes(5)); // 5 dakikadan sonraki güncellemeler

            foreach (var ticket in statusChangedTickets.OrderByDescending(x => x.UpdatedAt).Take(10))
            {
                // Türkçe enum değerleri kullanarak color assignment
                string statusColor = ticket.Status switch
                {
                    Domain.Enums.TicketStatus.İnceleniyor => "bg-yellow-500",
                    Domain.Enums.TicketStatus.İşlemde => "bg-blue-500",
                    Domain.Enums.TicketStatus.Çözüldü => "bg-green-500",
                    Domain.Enums.TicketStatus.Kapandı => "bg-gray-500",
                    Domain.Enums.TicketStatus.Reddedildi => "bg-red-500",
                    _ => "bg-gray-400"
                };

                // Türkçe status text
                string statusText = ticket.Status switch
                {
                    Domain.Enums.TicketStatus.İnceleniyor => "İnceleniyor",
                    Domain.Enums.TicketStatus.İşlemde => "İşlemde",
                    Domain.Enums.TicketStatus.Çözüldü => "Çözüldü",
                    Domain.Enums.TicketStatus.Kapandı => "Kapandı",
                    Domain.Enums.TicketStatus.Reddedildi => "Reddedildi",
                    _ => "Güncellendi"
                };

                activities.Add(new RecentActivityDto
                {
                    Type = "status_changed",
                    Title = $"Durum güncellendi: {statusText}",
                    Description = $"#{ticket.TicketNumber} - {ticket.Title}",
                    CreatedAt = ticket.UpdatedAt ?? ticket.CreatedAt,
                    Color = statusColor
                });
            }

            return activities.OrderByDescending(x => x.CreatedAt).Take(20).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent activities");
            return new List<RecentActivityDto>();
        }
    }

    // AJAX endpoint for real-time updates
    [HttpGet]
    public async Task<JsonResult> GetLatestStats()
    {
        try
        {
            var statsQuery = new GetDashboardStatsQuery();
            var result = await _mediator.Send(statsQuery);

            if (result.IsSuccess)
            {
                return Json(new { success = true, data = result.Data });
            }

            return Json(new { success = false, error = "İstatistikler yüklenemedi" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting latest stats");
            return Json(new { success = false, error = "Beklenmeyen hata" });
        }
    }
}

// Enhanced DTOs
public class RecentActivityDto
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Color { get; set; } = string.Empty;
}

public class CustomerInfoDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}