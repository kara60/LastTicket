using MediatR;
using TicketSystem.Application.Common.Models;

namespace TicketSystem.Application.Common.Commands;

public interface ICommand : IRequest<Result>
{
}

public interface ICommand<TResponse> : IRequest<Result<TResponse>>
{
}