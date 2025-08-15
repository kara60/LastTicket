using TicketSystem.Application.Common.Queries;
using TicketSystem.Application.Features.Common.DTOs;

namespace TicketSystem.Application.Features.TicketTypes.Queries.GetTicketTypes;

public class GetTicketTypesQuery : IQuery<List<TicketTypeDto>>
{
    public bool OnlyActive { get; set; } = true;
}