using Microsoft.Extensions.Logging;
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
    private readonly ILogger<UpdateTicketStatusCommandHandler> _logger;

    public UpdateTicketStatusCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IPmoIntegrationService pmoIntegrationService,
        ILogger<UpdateTicketStatusCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _pmoIntegrationService = pmoIntegrationService;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateTicketStatusCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("=== UpdateTicketStatus Handler Start ===");
            _logger.LogInformation("TicketId: {TicketId}, NewStatus: {NewStatus}, SendToPmo: {SendToPmo}",
                request.TicketId, request.NewStatus, request.SendToPmo);

            // Only admin can update status
            if (!_currentUserService.IsAdmin)
            {
                _logger.LogWarning("User is not admin: {UserId}", _currentUserService.UserId);
                return Result.Failure("Bu işlem için yetkiniz bulunmamaktadır.");
            }

            if (!_currentUserService.UserId.HasValue)
            {
                _logger.LogError("Current user ID is null");
                return Result.Failure("Kullanıcı bilgisi alınamadı.");
            }

            var currentUserId = _currentUserService.UserId.Value;

            // Load ticket with company info
            var ticket = await _unitOfWork.Tickets.FirstOrDefaultAsync(
                x => x.Id == request.TicketId && x.CompanyId == _currentUserService.CompanyId,
                x => x.Company);

            if (ticket == null)
            {
                _logger.LogError("Ticket not found: {TicketId}", request.TicketId);
                return Result.Failure("Ticket bulunamadı.");
            }

            var oldStatus = ticket.Status;
            var oldStatusDisplay = GetStatusDisplay(oldStatus);
            var newStatusDisplay = GetStatusDisplay(request.NewStatus);

            _logger.LogInformation("Status change: {OldStatus} -> {NewStatus}", oldStatus, request.NewStatus);

            // Update ticket status
            ticket.Status = request.NewStatus;
            ticket.UpdatedAt = DateTime.UtcNow;
            ticket.UpdatedBy = currentUserId.ToString();

            // Set specific dates based on status
            switch (request.NewStatus)
            {
                case TicketStatus.İşlemde:
                    if (oldStatus == TicketStatus.İnceleniyor)
                    {
                        ticket.ApprovedAt = DateTime.UtcNow;
                    }
                    break;

                case TicketStatus.Çözüldü:
                    ticket.ResolvedAt = DateTime.UtcNow;
                    break;

                case TicketStatus.Kapandı:
                    ticket.ClosedAt = DateTime.UtcNow;
                    break;

                case TicketStatus.Reddedildi:
                    ticket.RejectedAt = DateTime.UtcNow;
                    break;
            }

            // Update ticket in repository
            _unitOfWork.Tickets.Update(ticket);

            // Add comment if provided
            if (!string.IsNullOrWhiteSpace(request.Comment))
            {
                _logger.LogInformation("Adding status change comment");

                var comment = new TicketComment
                {
                    TicketId = ticket.Id,
                    UserId = currentUserId,
                    Content = request.Comment.Trim(),
                    IsInternal = false,
                    IsSystemGenerated = false,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = currentUserId.ToString()
                };

                await _unitOfWork.TicketComments.AddAsync(comment);
            }

            // Create history entry - LET EF CORE GENERATE THE ID
            _logger.LogInformation("Creating history entry");

            var historyEntry = new TicketHistory
            {
                // DON'T SET ID - Let EF Core auto-generate it
                TicketId = ticket.Id,
                Action = $"Durum değiştirildi: {oldStatusDisplay} → {newStatusDisplay}",
                OldValue = oldStatusDisplay,
                NewValue = newStatusDisplay,
                UserId = currentUserId,
                Description = !string.IsNullOrWhiteSpace(request.Comment)
                    ? $"Durum değiştirme notu: {request.Comment.Trim()}"
                    : "Durum değiştirildi",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUserId.ToString()
            };

            await _unitOfWork.TicketHistory.AddAsync(historyEntry);

            // PMO Integration if requested
            if (request.SendToPmo &&
                request.NewStatus == TicketStatus.İşlemde &&
                ticket.Company != null &&
                ticket.Company.RequiresPMOIntegration &&
                !string.IsNullOrEmpty(ticket.Company.PMOApiEndpoint))
            {
                _logger.LogInformation("Sending to PMO");

                try
                {
                    var pmoResult = await _pmoIntegrationService.SendTicketToPmoAsync(
                        ticket.Id,
                        ticket.Company.PMOApiEndpoint,
                        ticket.Company.PMOApiKey ?? "");

                    if (!pmoResult)
                    {
                        _logger.LogError("PMO integration failed");
                        // Continue with status update even if PMO fails

                        // Add a system comment about PMO failure
                        var pmoFailComment = new TicketComment
                        {
                            TicketId = ticket.Id,
                            UserId = currentUserId,
                            Content = "PMO entegrasyonu sırasında hata oluştu. Ticket durumu güncellenmiş ancak PMO'ya gönderilememiştir.",
                            IsInternal = true,
                            IsSystemGenerated = true,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = currentUserId.ToString()
                        };

                        await _unitOfWork.TicketComments.AddAsync(pmoFailComment);
                    }
                    else
                    {
                        _logger.LogInformation("PMO integration successful");

                        // Add success comment
                        var pmoSuccessComment = new TicketComment
                        {
                            TicketId = ticket.Id,
                            UserId = currentUserId,
                            Content = "Ticket başarıyla PMO sistemine gönderilmiştir.",
                            IsInternal = true,
                            IsSystemGenerated = true,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = currentUserId.ToString()
                        };

                        await _unitOfWork.TicketComments.AddAsync(pmoSuccessComment);
                    }
                }
                catch (Exception pmoEx)
                {
                    _logger.LogError(pmoEx, "PMO integration exception");
                    // Continue with status update
                }
            }

            // Save all changes in one transaction
            _logger.LogInformation("Saving all changes to database");
            var savedCount = await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Successfully saved {Count} entities", savedCount);
            _logger.LogInformation("=== UpdateTicketStatus Handler Success ===");

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ticket status: {Message}", ex.Message);
            _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);

            if (ex.InnerException != null)
            {
                _logger.LogError("Inner exception: {InnerMessage}", ex.InnerException.Message);
            }

            return Result.Failure($"Ticket durumu güncellenirken hata oluştu: {ex.Message}");
        }
    }

    private static string GetStatusDisplay(TicketStatus status)
    {
        return status switch
        {
            TicketStatus.İnceleniyor => "İnceleniyor",
            TicketStatus.İşlemde => "İşlemde",
            TicketStatus.Çözüldü => "Çözüldü",
            TicketStatus.Kapandı => "Kapandı",
            TicketStatus.Reddedildi => "Reddedildi",
            _ => status.ToString()
        };
    }
}