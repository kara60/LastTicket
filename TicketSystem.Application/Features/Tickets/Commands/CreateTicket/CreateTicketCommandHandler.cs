using AutoMapper;
using TicketSystem.Application.Common.Exceptions;
using TicketSystem.Application.Common.Handlers;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Application.Common.Models;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;

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
        int? customerId = null;
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
            CompanyId = _currentUserService.CompanyId.Value,
            CustomerId = customerId,
            CreatedByUserId = _currentUserService.UserId!.Value,
            TicketTypeId = request.TypeId,
            TicketCategoryId = request.CategoryId,
            TicketNumber = ticketNumber,
            Title = request.Title,
            Description = request.Description,
            FormData = System.Text.Json.JsonSerializer.Serialize(request.FormData),
            SelectedModule = request.SelectedModule,
            Status = TicketStatus.İnceleniyor
        };

        // Use the Create method from domain entity
        ticket.Create();

        await _unitOfWork.Tickets.AddAsync(ticket);

        // Add history record
        var history = TicketHistory.CreateStatusChange(
            ticket.Id,
            _currentUserService.UserId.Value,
            TicketStatus.İnceleniyor,
            TicketStatus.İnceleniyor);

        await _unitOfWork.TicketHistory.AddAsync(history);
        await _unitOfWork.SaveChangesAsync();

        return Result<string>.Success(ticketNumber);
    }
}