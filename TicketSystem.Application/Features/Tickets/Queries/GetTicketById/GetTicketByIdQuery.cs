using TicketSystem.Application.Common.Queries;
using TicketSystem.Application.Features.Tickets.DTOs;

namespace TicketSystem.Application.Features.Tickets.Queries.GetTicketById;

public class GetTicketByIdQuery : IQuery<TicketDto>
{
    public int Id { get; set; }

    public GetTicketByIdQuery(int id)
    {
        Id = id;
    }
}