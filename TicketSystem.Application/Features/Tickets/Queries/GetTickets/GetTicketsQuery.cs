using TicketSystem.Application.Common.Models;
using TicketSystem.Application.Common.Queries;
using TicketSystem.Application.Features.Tickets.DTOs;
using TicketSystem.Domain.Enums;

namespace TicketSystem.Application.Features.Tickets.Queries.GetTickets;

public class GetTicketsQuery : IQuery<PaginatedList<TicketListDto>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public int? CustomerId { get; set; }
    public int? TypeId { get; set; }
    public int? CategoryId { get; set; }
    public TicketStatus? Status { get; set; }
    public int? AssignedToId { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public string SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
}