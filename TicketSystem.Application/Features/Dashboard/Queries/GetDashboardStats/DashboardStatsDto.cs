namespace TicketSystem.Application.Features.Dashboard.Queries.GetDashboardStats
{
    public class DashboardStatsDto
    {
        public int TotalTickets { get; set; }
        public int ActiveTickets { get; set; }
        public int ResolvedTickets { get; set; }
        public int ClosedTickets { get; set; }
        public List<TicketsByStatusDto> TicketsByStatus { get; set; } = new();
        public List<TicketsByTypeDto> TicketsByType { get; set; } = new();
        public List<TicketsByCategoryDto> TicketsByCategory { get; set; } = new();
        public List<TicketsByCustomerDto> TicketsByCustomer { get; set; } = new();
        public List<TicketsTrendDto> TicketsTrend { get; set; } = new();
    }

    public class TicketsByStatusDto
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
        public string Color { get; set; } = string.Empty;
    }

    public class TicketsByTypeDto
    {
        public string TypeName { get; set; } = string.Empty;
        public int Count { get; set; }
        public string Color { get; set; } = string.Empty;
    }

    public class TicketsByCategoryDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public int Count { get; set; }
        public string Color { get; set; } = string.Empty;
    }

    public class TicketsByCustomerDto
    {
        public string CustomerName { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class TicketsTrendDto
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }
}