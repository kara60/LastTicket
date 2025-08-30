namespace TicketSystem.Application.Features.Dashboard.Queries.GetDashboardStats
{
    public class DashboardStatsDto
    {
        public int TotalTickets { get; set; }
        public int ActiveTickets { get; set; }
        public int ResolvedTickets { get; set; }
        public int ClosedTickets { get; set; }
        public int? RejectedTickets { get; set; } // Yeni eklendi
        public List<TicketsByStatusDto> TicketsByStatus { get; set; } = new();
        public List<TicketsByTypeDto> TicketsByType { get; set; } = new();
        public List<TicketsByCategoryDto> TicketsByCategory { get; set; } = new();
        public List<TicketsByCustomerDto> TicketsByCustomer { get; set; } = new();
        public List<TicketsTrendDto> TicketsTrend { get; set; } = new();
        public List<LongTermTicketDto>? LongTermTickets { get; set; } = new(); // Yeni eklendi
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
        public int? ActiveCount { get; set; } // Yeni eklendi
        public int? ResolvedCount { get; set; } // Yeni eklendi
    }

    public class TicketsTrendDto
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
        public int CreatedCount { get; set; } // Yeni eklendi
        public int? ResolvedCount { get; set; } // Yeni eklendi
    }

    // Yeni DTO - 15+ gün bekleyen ticketlar için
    public class LongTermTicketDto
    {
        public int Id { get; set; }
        public string TicketNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public string TypeColor { get; set; } = string.Empty;
        public int DaysInResolution { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}