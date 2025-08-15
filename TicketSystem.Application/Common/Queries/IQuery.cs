using MediatR;
using TicketSystem.Application.Common.Models;

namespace TicketSystem.Application.Common.Queries;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}