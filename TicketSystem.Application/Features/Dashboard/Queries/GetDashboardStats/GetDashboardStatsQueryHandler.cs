using TicketSystem.Application.Common.Handlers;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Application.Common.Models;
using TicketSystem.Domain.Enums;

namespace TicketSystem.Application.Features.Dashboard.Queries.GetDashboardStats;

public class GetDashboardStatsQueryHandler : IQueryHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetDashboardStatsQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result<DashboardStatsDto>> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.CompanyId.HasValue)
        {
            return Result<DashboardStatsDto>.Failure("Kullanıcı doğrulanamadı.");
        }

        var companyId = _currentUserService.CompanyId.Value;

        // Get tickets based on user role
        var tickets = await _unitOfWork.Tickets.FindAsync(
            x => x.CompanyId == companyId &&
                 (!_currentUserService.IsCustomer || x.CustomerId == _currentUserService.CustomerId),
            x => x.Type,
            x => x.Category!,
            x => x.Customer!
        );

        var ticketsList = tickets.ToList();

        // Calculate stats
        var totalTickets = ticketsList.Count;
        var activeTickets = ticketsList.Count(t => t.Status == TicketStatus.İnceleniyor || t.Status == TicketStatus.İşlemde);
        var resolvedTickets = ticketsList.Count(t => t.Status == TicketStatus.Çözüldü);
        var closedTickets = ticketsList.Count(t => t.Status == TicketStatus.Kapandı);
        var rejectedTickets = ticketsList.Count(t => t.Status == TicketStatus.Reddedildi);

        // Tickets by status
        var ticketsByStatus = Enum.GetValues<TicketStatus>()
            .Select(status => new TicketsByStatusDto
            {
                Status = GetStatusDisplay(status),
                Count = ticketsList.Count(t => t.Status == status),
                Color = GetStatusColor(status)
            })
            .Where(x => x.Count > 0)
            .ToList();

        // Tickets by type
        var ticketsByType = ticketsList
            .GroupBy(t => t.Type.Name)
            .Select(g => new TicketsByTypeDto
            {
                TypeName = g.Key,
                Count = g.Count(),
                Color = g.First().Type.Color
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        // Tickets by category
        var ticketsByCategory = ticketsList
            .Where(t => t.Category != null)
            .GroupBy(t => t.Category!.Name)
            .Select(g => new TicketsByCategoryDto
            {
                CategoryName = g.Key,
                Count = g.Count(),
                Color = g.First().Category!.Color
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        // Tickets by customer (only for admin) - ENHANCED
        var ticketsByCustomer = new List<TicketsByCustomerDto>();
        if (_currentUserService.IsAdmin)
        {
            ticketsByCustomer = ticketsList
                .Where(t => t.Customer != null)
                .GroupBy(t => t.Customer!.Name)
                .Select(g => new TicketsByCustomerDto
                {
                    CustomerName = g.Key,
                    Count = g.Count(),
                    ActiveCount = g.Count(t => t.Status == TicketStatus.İnceleniyor || t.Status == TicketStatus.İşlemde),
                    ResolvedCount = g.Count(t => t.Status == TicketStatus.Çözüldü || t.Status == TicketStatus.Kapandı)
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList();
        }

        // Tickets trend (last 30 days) - ENHANCED
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var trendTickets = ticketsList.Where(t => t.CreatedAt >= thirtyDaysAgo || t.UpdatedAt >= thirtyDaysAgo);

        var ticketsTrend = Enumerable.Range(0, 30)
            .Select(i => thirtyDaysAgo.AddDays(i).Date)
            .Select(date => new TicketsTrendDto
            {
                Date = date,
                Count = ticketsList.Count(t => t.CreatedAt.Date == date), // Backward compatibility
                CreatedCount = ticketsList.Count(t => t.CreatedAt.Date == date),
                ResolvedCount = ticketsList.Count(t => t.UpdatedAt.HasValue &&
                    t.UpdatedAt.Value.Date == date &&
                    (t.Status == TicketStatus.Çözüldü || t.Status == TicketStatus.Kapandı))
            })
            .OrderBy(x => x.Date)
            .ToList();

        // Long-term tickets (15+ days in resolution) - NEW
        var fifteenDaysAgo = DateTime.UtcNow.AddDays(-15);
        var longTermTickets = ticketsList
            .Where(t => t.Status == TicketStatus.Çözüldü &&
                   t.UpdatedAt.HasValue &&
                   t.UpdatedAt.Value <= fifteenDaysAgo)
            .Select(t => new LongTermTicketDto
            {
                Id = t.Id,
                TicketNumber = t.TicketNumber,
                Title = t.Title,
                CustomerName = t.Customer?.Name ?? "N/A",
                TypeName = t.Type.Name,
                TypeColor = t.Type.Color,
                DaysInResolution = t.UpdatedAt.HasValue
                    ? (int)(DateTime.UtcNow - t.UpdatedAt.Value).TotalDays
                    : 0,
                LastUpdated = t.UpdatedAt ?? t.CreatedAt
            })
            .OrderByDescending(x => x.DaysInResolution)
            .ToList();

        var result = new DashboardStatsDto
        {
            TotalTickets = totalTickets,
            ActiveTickets = activeTickets,
            ResolvedTickets = resolvedTickets,
            ClosedTickets = closedTickets,
            RejectedTickets = rejectedTickets,
            TicketsByStatus = ticketsByStatus,
            TicketsByType = ticketsByType,
            TicketsByCategory = ticketsByCategory,
            TicketsByCustomer = ticketsByCustomer,
            TicketsTrend = ticketsTrend,
            LongTermTickets = longTermTickets
        };

        return Result<DashboardStatsDto>.Success(result);
    }

    private static string GetStatusDisplay(TicketStatus status)
    {
        return status switch
        {
            TicketStatus.İnceleniyor => "İnceleniyor",
            TicketStatus.İşlemde => "İşlemde",
            TicketStatus.Çözüldü => "Çözüldü",
            TicketStatus.Kapandı => "Kapandı",
            TicketStatus.Reddedildi => "Reddedildi",
            _ => status.ToString()
        };
    }

    private static string GetStatusColor(TicketStatus status)
    {
        return status switch
        {
            TicketStatus.İnceleniyor => "#f59e0b", // Orange
            TicketStatus.İşlemde => "#3b82f6",     // Blue
            TicketStatus.Çözüldü => "#10b981",     // Green
            TicketStatus.Kapandı => "#6b7280",     // Gray
            TicketStatus.Reddedildi => "#ef4444",  // Red
            _ => "#6b7280"
        };
    }
}