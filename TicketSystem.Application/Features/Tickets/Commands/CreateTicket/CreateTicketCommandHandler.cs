using AutoMapper;
using TicketSystem.Application.Common.Exceptions;
using TicketSystem.Application.Common.Handlers;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Application.Common.Models;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;
using TicketSystem.Infrastructure.Repositories;

namespace TicketSystem.Application.Features.Tickets.Commands.CreateTicket;

public class CreateTicketCommandHandler : ICommandHandler<CreateTicketCommand, string>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public CreateTicketCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<Result<string>> Handle(CreateTicketCommand request, CancellationToken cancellationToken)
    {
        // Validate user
        if (!_currentUserService.IsAuthenticated || !_currentUserService.CompanyId.HasValue)
        {
            return Result<string>.Failure("Kullanıcı doğrulanamadı.");
        }

        // Validate ticket type
        var ticketType = await _unitOfWork.TicketTypes.FirstOrDefaultAsync(
            x => x.Id == request.TypeId && x.CompanyId == _currentUserService.CompanyId && x.IsActive);

        if (ticketType == null)
        {
            return Result<string>.Failure("Geçersiz ticket türü.");
        }

        // Validate ticket category
        var ticketCategory = await _unitOfWork.TicketCategories.FirstOrDefaultAsync(
            x => x.Id == request.CategoryId && x.CompanyId == _currentUserService.CompanyId && x.IsActive);

        if (ticketCategory == null)
        {
            return Result<string>.Failure("Geçersiz ticket kategorisi.");
        }

        // Get customer for customer users
        Guid? customerId = null;
        if (_currentUserService.IsCustomer && _currentUserService.CustomerId.HasValue)
        {
            customerId = _currentUserService.CustomerId.Value;
        }

        // Generate ticket number
        var lastTicket = await _unitOfWork.Tickets
            .FindAsync(x => x.CompanyId == _currentUserService.CompanyId);

        var ticketCount = lastTicket.Count() + 1;
        var ticketNumber = $"TKT-{DateTime.Now:yyyyMM}-{ticketCount:D4}";

        // Create ticket
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            CompanyId = _currentUserService.CompanyId.Value,
            CustomerId = customerId,
            CreatedById = _currentUserService.UserId!.Value,
            TypeId = request.TypeId,
            CategoryId = request.CategoryId,
            TicketNumber = ticketNumber,
            Title = request.Title,
            Description = request.Description,
            FormData = request.FormData,
            SelectedModule = request.SelectedModule,
            Status = TicketStatus.Inceleniyor
        };

        await _unitOfWork.Tickets.AddAsync(ticket);

        // Add history record
        var history = new TicketHistory
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            UserId = _currentUserService.UserId.Value,
            Action = "Ticket Oluşturuldu",
            Description = $"Ticket oluşturuldu. Durum: {TicketStatus.Inceleniyor}"
        };

        await _unitOfWork.TicketHistory.AddAsync(history);
        await _unitOfWork.SaveChangesAsync();

        return Result<string>.Success(ticketNumber);
    }
}