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

        // Tickets by customer (only for admin)
        var ticketsByCustomer = new List<TicketsByCustomerDto>();
        if (_currentUserService.IsAdmin)
        {
            ticketsByCustomer = ticketsList
                .Where(t => t.Customer != null)
                .GroupBy(t => t.Customer!.Name)
                .Select(g => new TicketsByCustomerDto
                {
                    CustomerName = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList();
        }

        // Tickets trend (last 30 days)
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var ticketsTrend = ticketsList
            .Where(t => t.CreatedAt >= thirtyDaysAgo)
            .GroupBy(t => t.CreatedAt.Date)
            .Select(g => new TicketsTrendDto
            {
                Date = g.Key,
                Count = g.Count()
            })
            .OrderBy(x => x.Date)
            .ToList();

        var result = new DashboardStatsDto
        {
            TotalTickets = totalTickets,
            ActiveTickets = activeTickets,
            ResolvedTickets = resolvedTickets,
            ClosedTickets = closedTickets,
            TicketsByStatus = ticketsByStatus,
            TicketsByType = ticketsByType,
            TicketsByCategory = ticketsByCategory,
            TicketsByCustomer = ticketsByCustomer,
            TicketsTrend = ticketsTrend
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