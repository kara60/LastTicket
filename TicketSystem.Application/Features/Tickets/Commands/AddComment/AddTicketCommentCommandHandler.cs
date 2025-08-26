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
        Console.WriteLine("=== AddTicketCommentCommandHandler Debug ===");
        Console.WriteLine($"Request TicketId: {request.TicketId}");
        Console.WriteLine($"Request Content: '{request.Content}'");
        Console.WriteLine($"Request IsInternal: {request.IsInternal}");

        // User authentication check
        Console.WriteLine($"IsAuthenticated: {_current.IsAuthenticated}");
        Console.WriteLine($"UserId: {_current.UserId}");
        Console.WriteLine($"CompanyId: {_current.CompanyId}");

        if (!_current.IsAuthenticated || !_current.UserId.HasValue || !_current.CompanyId.HasValue)
        {
            Console.WriteLine("ERROR: User authentication failed");
            return Result<int>.Failure("Kullanıcı doğrulanamadı.");
        }

        Console.WriteLine($"Authenticated user - UserId: {_current.UserId.Value}, CompanyId: {_current.CompanyId.Value}");

        try
        {
            // Ticket existence check
            var ticket = await _uow.Tickets.FirstOrDefaultAsync(
                x => x.Id == request.TicketId && x.CompanyId == _current.CompanyId);

            Console.WriteLine($"Ticket found: {ticket != null}");
            if (ticket != null)
            {
                Console.WriteLine($"Ticket details - Id: {ticket.Id}, Number: {ticket.TicketNumber}, CompanyId: {ticket.CompanyId}");
            }

            if (ticket == null)
            {
                Console.WriteLine("ERROR: Ticket not found or doesn't belong to user's company");
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

            Console.WriteLine($"Creating comment - TicketId: {comment.TicketId}, UserId: {comment.UserId}");

            // Add comment to database
            await _uow.TicketComments.AddAsync(comment);
            Console.WriteLine("Comment added to repository");

            // Save changes
            var savedCount = await _uow.SaveChangesAsync();
            Console.WriteLine($"SaveChanges result: {savedCount} entities saved");

            if (savedCount > 0)
            {
                Console.WriteLine($"SUCCESS: Comment created with ID: {comment.Id}");
                return Result<int>.Success(comment.Id);
            }
            else
            {
                Console.WriteLine("ERROR: No entities were saved");
                return Result<int>.Failure("Yorum kaydedilemedi.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"EXCEPTION in AddTicketCommentCommandHandler: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return Result<int>.Failure($"Yorum eklenirken hata oluştu: {ex.Message}");
        }
    }
}