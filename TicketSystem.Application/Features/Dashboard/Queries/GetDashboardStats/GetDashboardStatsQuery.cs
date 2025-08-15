using TicketSystem.Application.Common.Queries;

namespace TicketSystem.Application.Features.Dashboard.Queries.GetDashboardStats;

public class GetDashboardStatsQuery : IQuery<DashboardStatsDto>
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}