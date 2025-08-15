using TicketSystem.Application.Common.Handlers;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Application.Common.Models;
using TicketSystem.Domain.Entities;

namespace TicketSystem.Application.Features.Tickets.Commands.AddComment;

public class AddTicketCommentCommandHandler : ICommandHandler<AddTicketCommentCommand, int>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _current;

    public AddTicketCommentCommandHandler(IUnitOfWork uow, ICurrentUserService current)
    {
        _uow = uow;
        _current = current;
    }

    public async Task<Result<int>> Handle(AddTicketCommentCommand request, CancellationToken cancellationToken)
    {
        if (!_current.IsAuthenticated || !_current.UserId.HasValue || !_current.CompanyId.HasValue)
            return Result<int>.Failure("Kullanıcı doğrulanamadı.");

        var ticket = await _uow.Tickets.FirstOrDefaultAsync(
            x => x.Id == request.TicketId && x.CompanyId == _current.CompanyId);

        if (ticket == null)
            return Result<int>.Failure("Ticket bulunamadı.");

        var comment = new TicketComment
        {
            Content = request.Content,
            IsInternal = request.IsInternal,
            TicketId = ticket.Id,
            UserId = _current.UserId.Value
        };

        await _uow.TicketComments.AddAsync(comment);
        await _uow.SaveChangesAsync();

        return Result<int>.Success(comment.Id);
    }
}