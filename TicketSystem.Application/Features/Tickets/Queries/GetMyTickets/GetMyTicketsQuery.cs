using TicketSystem.Application.Common.Models;
using TicketSystem.Application.Common.Queries;
using TicketSystem.Application.Features.Tickets.DTOs;
using TicketSystem.Domain.Enums;

namespace TicketSystem.Application.Features.Tickets.Queries.GetMyTickets;

public class GetMyTicketsQuery : IQuery<PaginatedList<TicketListDto>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public Guid? TypeId { get; set; }
    public Guid? CategoryId { get; set; }
    public TicketStatus? Status { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public string SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
}