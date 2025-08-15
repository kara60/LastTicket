using TicketSystem.Application.Common.Queries;
using TicketSystem.Application.Features.Tickets.DTOs;

namespace TicketSystem.Application.Features.Tickets.Queries.GetTicketById;

public class GetTicketByIdQuery : IQuery<TicketDto>
{
    public Guid Id { get; set; }

    public GetTicketByIdQuery(Guid id)
    {
        Id = id;
    }
}