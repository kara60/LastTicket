using AutoMapper;
using Microsoft.Extensions.Logging;
using System.Text.Json;
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
    private readonly ILogger<CreateTicketCommandHandler> _logger;

    public CreateTicketCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMapper mapper,
        ILogger<CreateTicketCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<string>> Handle(CreateTicketCommand request, CancellationToken cancellationToken)
    {
        try
        {

            // ✅ Authentication check
            if (!_currentUserService.IsAuthenticated ||
                !_currentUserService.CompanyId.HasValue ||
                !_currentUserService.UserId.HasValue)
            {
                _logger.LogError("User authentication failed");
                return Result<string>.Failure("Kullanıcı doğrulanamadı.");
            }

            var currentUserId = _currentUserService.UserId.Value;
            var currentCompanyId = _currentUserService.CompanyId.Value;

            _logger.LogInformation("User authenticated: UserId={UserId}, CompanyId={CompanyId}",
                currentUserId, currentCompanyId);

            // ✅ Validations
            var ticketType = await _unitOfWork.TicketTypes.FirstOrDefaultAsync(
                x => x.Id == request.TypeId && x.CompanyId == currentCompanyId);
            if (ticketType == null)
            {
                _logger.LogError("TicketType not found: TypeId={TypeId}", request.TypeId);
                return Result<string>.Failure("Geçersiz ticket türü.");
            }

            var category = await _unitOfWork.TicketCategories.FirstOrDefaultAsync(
                x => x.Id == request.CategoryId && x.CompanyId == currentCompanyId);
            if (category == null)
            {
                _logger.LogError("TicketCategory not found: CategoryId={CategoryId}", request.CategoryId);
                return Result<string>.Failure("Geçersiz kategori.");
            }

            _logger.LogInformation("Validations passed: Type={TypeName}, Category={CategoryName}",
                ticketType.Name, category.Name);

            // ✅ Module handling
            int? ticketCategoryModuleId = null;
            if (!string.IsNullOrEmpty(request.SelectedModule))
            {
                var module = await _unitOfWork.TicketCategoryModules.FirstOrDefaultAsync(
                    x => x.Name == request.SelectedModule && x.TicketCategoryId == request.CategoryId);
                ticketCategoryModuleId = module?.Id;

                _logger.LogInformation("Module handling: SelectedModule={SelectedModule}, ModuleId={ModuleId}",
                    request.SelectedModule, ticketCategoryModuleId);
            }

            // ✅ Generate ticket number
            var ticketNumber = await GenerateTicketNumberAsync();
            _logger.LogInformation("Generated ticket number: {TicketNumber}", ticketNumber);

            // ✅ CRITICAL: FormData serialization with proper error handling
            string formDataJson = "{}"; // Default to empty JSON object

            if (request.FormData != null && request.FormData.Any())
            {
                try
                {
                    _logger.LogInformation("=== SERIALIZING FormData ===");
                    _logger.LogInformation("FormData entries to serialize: {Count}", request.FormData.Count);

                    // Clean FormData - remove null values and empty strings
                    var cleanFormData = new Dictionary<string, object>();
                    foreach (var kvp in request.FormData)
                    {
                        if (kvp.Key != null && kvp.Value != null)
                        {
                            var stringValue = kvp.Value.ToString()?.Trim();
                            if (!string.IsNullOrEmpty(stringValue))
                            {
                                cleanFormData[kvp.Key] = stringValue;
                                _logger.LogInformation("Clean FormData[{Key}] = '{Value}'", kvp.Key, stringValue);
                            }
                        }
                    }

                    if (cleanFormData.Any())
                    {
                        var jsonOptions = new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                            WriteIndented = false,
                            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                        };

                        formDataJson = JsonSerializer.Serialize(cleanFormData, jsonOptions);
                        _logger.LogInformation("Successfully serialized FormData: {Json}", formDataJson);
                    }
                    else
                    {
                        _logger.LogWarning("No clean FormData to serialize, using empty JSON");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "FormData serialization failed, using empty JSON");
                    formDataJson = "{}";
                }
            }
            else
            {
                _logger.LogInformation("FormData is null or empty, using default empty JSON");
            }

            // ✅ Create ticket entity
            var ticket = new Ticket
            {
                Title = request.Title?.Trim() ?? "",
                Description = request.Description?.Trim() ?? "",
                Status = TicketStatus.İnceleniyor,
                TicketNumber = ticketNumber,
                FormData = formDataJson, // Always JSON string, never null
                SelectedModule = request.SelectedModule?.Trim(),

                CompanyId = currentCompanyId,
                CustomerId = _currentUserService.CustomerId,
                TypeId = request.TypeId,
                CategoryId = request.CategoryId,
                TicketCategoryModuleId = ticketCategoryModuleId,
                CreatedByUserId = currentUserId,

                SubmittedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _logger.LogInformation("=== TICKET ENTITY CREATED ===");
            _logger.LogInformation("Title: '{Title}'", ticket.Title);
            _logger.LogInformation("FormData length: {Length}", ticket.FormData?.Length ?? 0);
            _logger.LogInformation("FormData content: {FormData}", ticket.FormData);

            // ✅ Save ticket
            await _unitOfWork.Tickets.AddAsync(ticket);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Ticket saved with ID: {TicketId}", ticket.Id);

            // ✅ Verification - read back from database
            var savedTicket = await _unitOfWork.Tickets.GetByIdAsync(ticket.Id);
            if (savedTicket != null)
            {
                _logger.LogInformation("=== SAVE VERIFICATION ===");
                _logger.LogInformation("Saved FormData: {FormData}", savedTicket.FormData);
                _logger.LogInformation("Saved FormData Length: {Length}", savedTicket.FormData?.Length ?? 0);

                // Test deserialization
                try
                {
                    if (!string.IsNullOrEmpty(savedTicket.FormData))
                    {
                        var deserializedData = JsonSerializer.Deserialize<Dictionary<string, object>>(savedTicket.FormData);
                        _logger.LogInformation("Deserialization test successful: {Count} entries", deserializedData?.Count ?? 0);

                        if (deserializedData != null)
                        {
                            foreach (var kvp in deserializedData)
                            {
                                _logger.LogInformation("Deserialized[{Key}] = '{Value}'", kvp.Key, kvp.Value);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Deserialization test failed");
                }
            }
            else
            {
                _logger.LogError("Could not retrieve saved ticket for verification");
            }

            _logger.LogInformation("=== COMMAND HANDLER SUCCESS ===");
            return Result<string>.Success(ticketNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Command handler failed");
            return Result<string>.Failure("Ticket oluşturulurken hata oluştu: " + ex.Message);
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