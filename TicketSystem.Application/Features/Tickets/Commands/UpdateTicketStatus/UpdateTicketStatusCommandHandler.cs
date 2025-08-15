using TicketSystem.Application.Common.Exceptions;
using TicketSystem.Application.Common.Handlers;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Application.Common.Models;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;

namespace TicketSystem.Application.Features.Tickets.Commands.UpdateTicketStatus;

public class UpdateTicketStatusCommandHandler : ICommandHandler<UpdateTicketStatusCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPmoIntegrationService _pmoIntegrationService;

    public UpdateTicketStatusCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IPmoIntegrationService pmoIntegrationService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _pmoIntegrationService = pmoIntegrationService;
    }

    public async Task<Result> Handle(UpdateTicketStatusCommand request, CancellationToken cancellationToken)
    {
        // Only admin can update status
        if (!_currentUserService.IsAdmin)
        {
            return Result.Failure("Bu işlem için yetkiniz bulunmamaktadır.");
        }

        var ticket = await _unitOfWork.Tickets.FirstOrDefaultAsync(
            x => x.Id == request.TicketId && x.CompanyId == _currentUserService.CompanyId,
            x => x.Company);

        if (ticket == null)
        {
            return Result.Failure("Ticket bulunamadı.");
        }

        var oldStatus = ticket.Status;
        ticket.Status = request.NewStatus;

        // If status is approved and PMO integration enabled, send to PMO
        if (request.SendToPmo &&
            request.NewStatus == TicketStatus.Islenmede &&
            ticket.Company.PmoIntegrationEnabled &&
            !string.IsNullOrEmpty(ticket.Company.PmoApiEndpoint))
        {
            var pmoResult = await _pmoIntegrationService.SendTicketToPmoAsync(
                ticket.Id,
                ticket.Company.PmoApiEndpoint,
                ticket.Company.PmoApiKey ?? "");

            if (!pmoResult)
            {
                return Result.Failure("PMO entegrasyonu sırasında hata oluştu.");
            }
        }

        _unitOfWork.Tickets.Update(ticket);

        // Add comment if provided
        if (!string.IsNullOrEmpty(request.Comment))
        {
            var comment = new TicketComment
            {
                Id = Guid.NewGuid(),
                TicketId = ticket.Id,
                UserId = _currentUserService.UserId!.Value,
                Content = request.Comment,
                IsInternal = true
            };

            await _unitOfWork.TicketComments.AddAsync(comment);
        }

        // Add history record
        var history = new TicketHistory
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            UserId = _currentUserService.UserId!.Value,
            Action = "Durum Değişikliği",
            OldValue = oldStatus.ToString(),
            NewValue = request.NewStatus.ToString(),
            Description = $"Ticket durumu {oldStatus} -> {request.NewStatus} olarak değiştirildi."
        };

        await _unitOfWork.TicketHistory.AddAsync(history);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }
}