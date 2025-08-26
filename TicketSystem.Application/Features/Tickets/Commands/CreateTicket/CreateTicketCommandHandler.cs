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

            // ✅ FIX 1: Kullanıcı doğrulaması daha sıkı
            if (!_currentUserService.IsAuthenticated ||
                !_currentUserService.CompanyId.HasValue ||
                !_currentUserService.UserId.HasValue)
            {
                Console.WriteLine($"Authentication failed - IsAuth: {_currentUserService.IsAuthenticated}, CompanyId: {_currentUserService.CompanyId}, UserId: {_currentUserService.UserId}");
                return Result<string>.Failure("Kullanıcı doğrulanamadı veya gerekli bilgiler eksik.");
            }

            var currentUserId = _currentUserService.UserId.Value;
            var currentCompanyId = _currentUserService.CompanyId.Value;

            Console.WriteLine($"User authenticated - UserId: {currentUserId}, CompanyId: {currentCompanyId}");

            // Type ve Category validation
            var ticketType = await _unitOfWork.TicketTypes.FirstOrDefaultAsync(
                x => x.Id == request.TypeId && x.CompanyId == currentCompanyId);

            if (ticketType == null)
            {
                Console.WriteLine($"TicketType not found: {request.TypeId}");
                return Result<string>.Failure("Geçersiz ticket türü.");
            }

            var category = await _unitOfWork.TicketCategories.FirstOrDefaultAsync(
                x => x.Id == request.CategoryId && x.CompanyId == currentCompanyId);

            if (category == null)
            {
                Console.WriteLine($"Category not found: {request.CategoryId}");
                return Result<string>.Failure("Geçersiz kategori.");
            }

            Console.WriteLine($"Type found: {ticketType.Name}, Category found: {category.Name}");

            // Ticket Number Generate
            var ticketNumber = await GenerateTicketNumberAsync();
            Console.WriteLine($"Generated ticket number: {ticketNumber}");

            // ✅ FIX 2: TicketCategoryModule ID belirleme
            int? ticketCategoryModuleId = null;
            if (!string.IsNullOrEmpty(request.SelectedModule))
            {
                var module = await _unitOfWork.TicketCategoryModules.FirstOrDefaultAsync(
                    x => x.Name == request.SelectedModule && x.TicketCategoryId == request.CategoryId);
                ticketCategoryModuleId = module?.Id;
                Console.WriteLine($"Selected module: {request.SelectedModule}, ModuleId: {ticketCategoryModuleId}");
            }

            // ✅ FIX 3: Ticket Entity - TÜM gerekli alanları doldur
            var ticket = new Ticket
            {
                Title = request.Title,
                Description = request.Description ?? "",
                Status = TicketStatus.İnceleniyor,
                TicketNumber = ticketNumber,
                FormData = request.FormData.Any() ? JsonSerializer.Serialize(request.FormData) : null,
                SelectedModule = request.SelectedModule,

                // ✅ Foreign Keys - hepsi zorunlu
                CompanyId = currentCompanyId,
                CustomerId = _currentUserService.CustomerId, // Nullable
                TypeId = request.TypeId,
                CategoryId = request.CategoryId,
                TicketCategoryModuleId = ticketCategoryModuleId, // Nullable
                CreatedByUserId = currentUserId, // ✅ ARTIK AYARLANIYOR

                // ✅ Dates
                SubmittedAt = DateTime.UtcNow, // ✅ ARTIK AYARLANIYOR

                // AssignedToUserId boş kalacak (nullable)
            };

            Console.WriteLine($"Ticket entity created with all required fields");
            Console.WriteLine($"CreatedByUserId: {ticket.CreatedByUserId}");
            Console.WriteLine($"SubmittedAt: {ticket.SubmittedAt}");

            // Ticket'ı kaydet
            await _unitOfWork.Tickets.AddAsync(ticket);
            await _unitOfWork.SaveChangesAsync();

            Console.WriteLine($"Ticket saved successfully with ID: {ticket.Id}");

            // ✅ FIX 4: History ekle - ayrı transaction
            try
            {
                var historyEntry = new TicketHistory
                {
                    TicketId = ticket.Id,
                    Action = "Ticket oluşturuldu",
                    OldValue = "",
                    NewValue = $"Ticket #{ticketNumber} oluşturuldu",
                    UserId = currentUserId, // ✅ ARTIK AYARLANIYOR
                    Description = $"Yeni ticket oluşturuldu. Tür: {ticketType.Name}, Kategori: {category.Name}"
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
            Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
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