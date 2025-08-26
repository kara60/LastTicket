using AutoMapper;
using System.Text.Json;
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
        try
        {
            Console.WriteLine($"=== CreateTicketCommandHandler START ===");
            Console.WriteLine($"TypeId: {request.TypeId}, CategoryId: {request.CategoryId}");
            Console.WriteLine($"Title: '{request.Title}'");

            if (!_currentUserService.IsAuthenticated || !_currentUserService.CompanyId.HasValue)
            {
                return Result<string>.Failure("Kullanıcı doğrulanamadı.");
            }

            // Type ve Category validation
            var ticketType = await _unitOfWork.TicketTypes.FirstOrDefaultAsync(
                x => x.Id == request.TypeId && x.CompanyId == _currentUserService.CompanyId);

            if (ticketType == null)
            {
                Console.WriteLine($"TicketType not found: {request.TypeId}");
                return Result<string>.Failure("Geçersiz ticket türü.");
            }

            var category = await _unitOfWork.TicketCategories.FirstOrDefaultAsync(
                x => x.Id == request.CategoryId && x.CompanyId == _currentUserService.CompanyId);

            if (category == null)
            {
                Console.WriteLine($"Category not found: {request.CategoryId}");
                return Result<string>.Failure("Geçersiz kategori.");
            }

            Console.WriteLine($"Type found: {ticketType.Name}, Category found: {category.Name}");

            // Ticket Number Generate
            var ticketNumber = await GenerateTicketNumberAsync();
            Console.WriteLine($"Generated ticket number: {ticketNumber}");

            // Ticket Entity Oluştur
            var ticket = new Ticket
            {
                Title = request.Title,
                Description = request.Description ?? "",
                Status = TicketStatus.İnceleniyor,
                TicketNumber = ticketNumber,
                FormData = request.FormData.Any() ? JsonSerializer.Serialize(request.FormData) : null,
                SelectedModule = request.SelectedModule,
                CompanyId = _currentUserService.CompanyId.Value,
                CustomerId = _currentUserService.CustomerId,
                TypeId = request.TypeId,
                CategoryId = request.CategoryId,
                //CreatedByUserId = _currentUserService.UserId,
                //SubmittedAt = DateTime.UtcNow
            };

            Console.WriteLine($"Ticket entity created, adding to database...");

            // SADECE Ticket'ı kaydet - History için ayrı transaction
            await _unitOfWork.Tickets.AddAsync(ticket);
            await _unitOfWork.SaveChangesAsync();

            Console.WriteLine($"Ticket saved with ID: {ticket.Id}");

            // SONRA History ekle - ayrı transaction
            try
            {
                var historyEntry = new TicketHistory
                {
                    TicketId = ticket.Id,  // Artık ID mevcut
                    Action = "Ticket oluşturuldu",
                    OldValue = "",
                    NewValue = $"Ticket #{ticketNumber} oluşturuldu",
                    //UserId = _currentUserService.UserId,
                    //CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.TicketHistory.AddAsync(historyEntry);
                await _unitOfWork.SaveChangesAsync();

                Console.WriteLine("History entry added successfully");
            }
            catch (Exception historyEx)
            {
                Console.WriteLine($"History creation failed (non-critical): {historyEx.Message}");
                // History başarısız olsa da ticket oluşturuldu, devam et
            }

            Console.WriteLine($"SUCCESS: Ticket created with number: {ticketNumber}");
            return Result<string>.Success(ticketNumber);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR in CreateTicketCommandHandler: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return Result<string>.Failure($"Ticket oluşturulurken hata: {ex.Message}");
        }
    }

    private async Task<string> GenerateTicketNumberAsync()
    {
        var today = DateTime.UtcNow.Date;
        var prefix = $"T{today:yyyyMMdd}";

        var count = await _unitOfWork.Tickets.CountAsync(x =>
            x.CompanyId == _currentUserService.CompanyId &&
            x.CreatedAt.Date == today);

        return $"{prefix}-{(count + 1):D4}";
    }
}