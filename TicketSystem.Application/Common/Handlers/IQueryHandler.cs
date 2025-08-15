using MediatR;
using TicketSystem.Application.Common.Models;
using TicketSystem.Application.Common.Queries;

namespace TicketSystem.Application.Common.Handlers;

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>
{
}