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
        {
            return Result<int>.Failure("Kullanıcı doğrulanamadı.");
        }

        try
        {
            // Ticket existence check
            var ticket = await _uow.Tickets.FirstOrDefaultAsync(
                x => x.Id == request.TicketId && x.CompanyId == _current.CompanyId);

            if (ticket == null)
            {
                return Result<int>.Failure("Ticket bulunamadı.");
            }

            // Create comment entity
            var comment = new TicketComment
            {
                Content = request.Content,
                IsInternal = request.IsInternal,
                TicketId = ticket.Id,
                UserId = _current.UserId.Value
            };

            // Add comment to database
            await _uow.TicketComments.AddAsync(comment);

            // Save changes
            var savedCount = await _uow.SaveChangesAsync();

            if (savedCount > 0)
            {
                return Result<int>.Success(comment.Id);
            }
            else
            {
                return Result<int>.Failure("Yorum kaydedilemedi.");
            }
        }
        catch (Exception ex)
        {
            return Result<int>.Failure($"Yorum eklenirken hata oluştu: {ex.Message}");
        }
    }
}