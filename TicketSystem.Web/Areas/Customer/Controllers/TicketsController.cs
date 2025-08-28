using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Application.Features.Common.DTOs;
using TicketSystem.Application.Features.TicketCategories.Queries.GetTicketCategories;
using TicketSystem.Application.Features.Tickets.Commands.AddComment;
using TicketSystem.Application.Features.Tickets.Commands.CreateTicket;
using TicketSystem.Application.Features.Tickets.Queries.GetMyTickets;
using TicketSystem.Application.Features.Tickets.Queries.GetTicketById;
using TicketSystem.Application.Features.TicketTypes.Queries.GetTicketTypes;
using TicketSystem.Web.Areas.Customer.Models;

namespace TicketSystem.Web.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Policy = "CustomerOnly")]
public class TicketsController : Controller
{
    private readonly IMediator _mediator;
    private readonly ILogger<TicketsController> _logger;

    public TicketsController(IMediator mediator, ILogger<TicketsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<IActionResult> Index(
        string? searchTerm = null,
        string? status = null,
        bool showClosed = false,
        int page = 1,
        int pageSize = 20,
        string sortBy = "CreatedAt",
        bool sortDescending = true)
    {
        try
        {
            _logger.LogInformation("Customer tickets index requested with filters: SearchTerm={SearchTerm}, Status={Status}, ShowClosed={ShowClosed}, Page={Page}, PageSize={PageSize}",
                searchTerm, status, showClosed, page, pageSize);

            // String status'ı enum'a convert et
            TicketSystem.Domain.Enums.TicketStatus? statusEnum = null;
            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<TicketSystem.Domain.Enums.TicketStatus>(status, out var parsedStatus))
                {
                    statusEnum = parsedStatus;
                }
            }

            var query = new GetMyTicketsQuery
            {
                SearchTerm = searchTerm,
                Status = statusEnum,
                Page = page,
                PageSize = pageSize,
                SortBy = sortBy,
                SortDescending = sortDescending
            };

            var result = await _mediator.Send(query);

            if (result.IsSuccess && result.Data != null)
            {
                // Eğer showClosed false ise, kapalı ticket'ları filtrele
                if (!showClosed)
                {
                    var filteredItems = result.Data.Items
                        .Where(t => t.StatusDisplay != "Kapandı")
                        .ToList();

                    // Yeni bir PaginatedList oluştur
                    var filteredResult = new TicketSystem.Application.Common.Models.PaginatedList<TicketSystem.Application.Features.Tickets.DTOs.TicketListDto>(
                        filteredItems,
                        filteredItems.Count, // Total count da filtrelenmiş olacak
                        page,
                        pageSize);

                    ViewData["CurrentSearchTerm"] = searchTerm;
                    ViewData["CurrentStatus"] = status;
                    ViewData["CurrentShowClosed"] = showClosed;
                    ViewData["CurrentPageSize"] = pageSize;

                    return View(filteredResult);
                }

                // Filter bilgilerini ViewData'ya ekle (JavaScript için)
                ViewData["CurrentSearchTerm"] = searchTerm;
                ViewData["CurrentStatus"] = status;
                ViewData["CurrentShowClosed"] = showClosed;
                ViewData["CurrentPageSize"] = pageSize;

                return View(result.Data);
            }
            else
            {
                _logger.LogWarning("Failed to load tickets: {Errors}", string.Join(", ", result.Errors));
                TempData["Error"] = "Ticket'lar yüklenirken bir hata oluştu.";

                // Empty result with error
                var emptyResult = new TicketSystem.Application.Common.Models.PaginatedList<TicketSystem.Application.Features.Tickets.DTOs.TicketListDto>(
                    new List<TicketSystem.Application.Features.Tickets.DTOs.TicketListDto>(),
                    0, page, pageSize);

                return View(emptyResult);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading customer tickets index");
            TempData["Error"] = "Beklenmeyen bir hata oluştu.";

            var emptyResult = new TicketSystem.Application.Common.Models.PaginatedList<TicketSystem.Application.Features.Tickets.DTOs.TicketListDto>(
                new List<TicketSystem.Application.Features.Tickets.DTOs.TicketListDto>(),
                0, page, pageSize);

            return View(emptyResult);
        }
    }

    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var result = await _mediator.Send(new GetTicketByIdQuery(id));

            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Errors.FirstOrDefault() ?? "Ticket bulunamadı.";
                return RedirectToAction("Index");
            }

            return View(result.Data);
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Ticket detayları yüklenirken bir hata oluştu.";
            return RedirectToAction("Index");
        }
    }

    // 4 Adımlı Ticket Oluşturma
    public async Task<IActionResult> Create()
    {
        try
        {
            var ticketTypes = await _mediator.Send(new GetTicketTypesQuery());
            return View(new CreateTicketStep1ViewModel
            {
                TicketTypes = ticketTypes.Data ?? new List<TicketSystem.Application.Features.Common.DTOs.TicketTypeDto>()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading create ticket page");
            TempData["Error"] = "Ticket oluşturma sayfası yüklenirken bir hata oluştu.";
            return RedirectToAction("Index");
        }
    }

    [HttpPost]
    public async Task<IActionResult> SelectType(int typeId)
    {
        try
        {
            var categories = await _mediator.Send(new GetTicketCategoriesQuery());

            if (!categories.IsSuccess)
            {
                TempData["Error"] = "Kategoriler yüklenirken hata: " + categories.Errors.FirstOrDefault();
                return RedirectToAction("Create");
            }

            var ticketTypes = await _mediator.Send(new GetTicketTypesQuery());

            var selectedType = ticketTypes.Data?.FirstOrDefault(x => x.Id == typeId);
            if (selectedType == null)
            {
                TempData["Error"] = "Geçersiz ticket türü.";
                return RedirectToAction("Create");
            }

            var model = new CreateTicketStep2ViewModel
            {
                SelectedTypeId = typeId,
                SelectedType = selectedType,
                Categories = categories.Data ?? new List<TicketCategoryDto>()
            };

            return View("SelectCategory", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SelectType for typeId: {TypeId}", typeId);
            TempData["Error"] = "Bir hata oluştu: " + ex.Message;
            return RedirectToAction("Create");
        }
    }

    [HttpPost]
    public async Task<IActionResult> SelectCategory(int typeId, int categoryId)
    {
        try
        {
            var ticketTypes = await _mediator.Send(new GetTicketTypesQuery());
            var categories = await _mediator.Send(new GetTicketCategoriesQuery());

            var selectedType = ticketTypes.Data?.FirstOrDefault(x => x.Id == typeId);
            var selectedCategory = categories.Data?.FirstOrDefault(x => x.Id == categoryId);

            if (selectedType == null || selectedCategory == null)
            {
                TempData["Error"] = "Geçersiz seçim.";
                return RedirectToAction("Create");
            }

            return View("FillForm", new CreateTicketStep3ViewModel
            {
                SelectedTypeId = typeId,
                SelectedCategoryId = categoryId,
                SelectedType = selectedType,
                SelectedCategory = selectedCategory
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SelectCategory for typeId: {TypeId}, categoryId: {CategoryId}", typeId, categoryId);
            TempData["Error"] = "Bir hata oluştu: " + ex.Message;
            return RedirectToAction("Create");
        }
    }

    [HttpPost]
    public async Task<IActionResult> PreviewTicket(CreateTicketStep3ViewModel model)
    {
        try
        {
            _logger.LogInformation("PreviewTicket requested for TypeId: {TypeId}, CategoryId: {CategoryId}",
                model.SelectedTypeId, model.SelectedCategoryId);

            // ✅ FIX 1: Request.Form'dan manuel parse
            _logger.LogInformation("=== FORM DATA COMPLETE DEBUG START ===");
            foreach (var key in Request.Form.Keys)
            {
                var value = Request.Form[key];
                _logger.LogInformation("Form[{Key}] = '{Value}'", key, value);
            }
            _logger.LogInformation("=== FORM DATA COMPLETE DEBUG END ===");

            // ✅ FIX 2: ID'leri düzelt
            if (model.SelectedTypeId == 0 && int.TryParse(Request.Form["SelectedTypeId"], out int typeId))
            {
                model.SelectedTypeId = typeId;
                _logger.LogInformation("Fixed SelectedTypeId: {TypeId}", typeId);
            }

            if (model.SelectedCategoryId == 0 && int.TryParse(Request.Form["SelectedCategoryId"], out int categoryId))
            {
                model.SelectedCategoryId = categoryId;
                _logger.LogInformation("Fixed SelectedCategoryId: {CategoryId}", categoryId);
            }

            // ✅ FIX 3: FormData'yı yeni method ile parse et
            model.ParseFormDataFromRequest(Request.Form);

            // ✅ FIX 4: SelectedModule'ü al
            if (Request.Form.ContainsKey("SelectedModule"))
            {
                model.SelectedModule = Request.Form["SelectedModule"].ToString();
                _logger.LogInformation("SelectedModule: '{SelectedModule}'", model.SelectedModule);
            }

            // ✅ FIX 5: Type ve Category yükle
            var ticketTypes = await _mediator.Send(new GetTicketTypesQuery());
            var categories = await _mediator.Send(new GetTicketCategoriesQuery());

            model.SelectedType = ticketTypes.Data?.FirstOrDefault(x => x.Id == model.SelectedTypeId);
            model.SelectedCategory = categories.Data?.FirstOrDefault(x => x.Id == model.SelectedCategoryId);

            if (model.SelectedType == null || model.SelectedCategory == null)
            {
                _logger.LogError("SelectedType or SelectedCategory is null after loading");
                TempData["Error"] = "Seçilen tür veya kategori bulunamadı.";
                return RedirectToAction("Create");
            }

            _logger.LogInformation("Loaded Type: {TypeName}, Category: {CategoryName}",
                model.SelectedType.Name, model.SelectedCategory.Name);

            // ✅ FIX 6: Title oluştur
            if (string.IsNullOrWhiteSpace(model.Title))
            {
                model.Title = model.GetDynamicTitle();
                _logger.LogInformation("Generated dynamic title: '{Title}'", model.Title);
            }

            // ✅ FIX 7: PreviewModel oluştur
            var previewModel = new CreateTicketStep4ViewModel
            {
                SelectedTypeId = model.SelectedTypeId,
                SelectedCategoryId = model.SelectedCategoryId,
                Title = model.Title,
                Description = model.Description,
                SelectedModule = model.SelectedModule,
                FormData = new Dictionary<string, object>(model.FormData), // Deep copy
                SelectedType = model.SelectedType,
                SelectedCategory = model.SelectedCategory
            };

            _logger.LogInformation("PreviewModel created with {FormDataCount} form fields", previewModel.FormData.Count);

            // ✅ FIX 8: FormData içeriğini logla
            foreach (var kvp in previewModel.FormData)
            {
                _logger.LogInformation("PreviewModel FormData[{Key}] = '{Value}' (Type: {Type})",
                    kvp.Key, kvp.Value, kvp.Value?.GetType().Name ?? "null");
            }

            return View("Preview", previewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in PreviewTicket");
            TempData["Error"] = "Önizleme oluşturulurken bir hata oluştu: " + ex.Message;
            return RedirectToAction("Create");
        }
    }

    [HttpPost]
    public async Task<IActionResult> SubmitTicket(CreateTicketStep4ViewModel model)
    {
        try
        {
            Console.WriteLine("=== SUBMIT TICKET JSON APPROACH ===");

            // JSON string'den FormData'yı parse et
            if (Request.Form.ContainsKey("FormDataJson"))
            {
                var formDataJson = Request.Form["FormDataJson"].ToString();
                Console.WriteLine($"FormDataJson: {formDataJson}");

                if (!string.IsNullOrEmpty(formDataJson))
                {
                    try
                    {
                        var deserializedData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(formDataJson);
                        if (deserializedData != null)
                        {
                            model.FormData = deserializedData;
                            Console.WriteLine($"JSON'dan parse edildi: {model.FormData.Count} adet");

                            foreach (var kvp in model.FormData)
                            {
                                Console.WriteLine($"Parsed: {kvp.Key} = {kvp.Value}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"JSON parse hatası: {ex.Message}");
                    }
                }
            }

            // Title generate et
            if (string.IsNullOrWhiteSpace(model.Title))
            {
                model.Title = $"Ticket - {DateTime.Now:dd.MM.yyyy HH:mm}";
            }

            var command = new CreateTicketCommand
            {
                TypeId = model.SelectedTypeId,
                CategoryId = model.SelectedCategoryId,
                Title = model.Title,
                Description = model.Description,
                SelectedModule = model.SelectedModule,
                FormData = model.FormData ?? new Dictionary<string, object>()
            };

            Console.WriteLine($"Command FormData count: {command.FormData.Count}");

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                TempData["Success"] = $"Ticket başarıyla oluşturuldu. Ticket No: {result.Data}";
                return RedirectToAction("Index");
            }

            TempData["Error"] = result.Errors.FirstOrDefault() ?? "Ticket oluşturulamadı.";
            return RedirectToAction("Create");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SubmitTicket error: {ex.Message}");
            TempData["Error"] = "Ticket oluşturulurken bir hata oluştu.";
            return RedirectToAction("Create");
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddComment(int ticketId, string content)
    {
        try
        {
            _logger.LogInformation("AddComment requested for TicketId: {TicketId}, ContentLength: {ContentLength}",
                ticketId, content?.Length ?? 0);

            // Validation
            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("AddComment failed: Empty content for TicketId: {TicketId}", ticketId);
                TempData["Error"] = "Yorum içeriği boş olamaz.";
                return RedirectToAction("Details", new { id = ticketId });
            }

            if (ticketId <= 0)
            {
                _logger.LogWarning("AddComment failed: Invalid TicketId: {TicketId}", ticketId);
                TempData["Error"] = "Geçersiz ticket ID.";
                return RedirectToAction("Index");
            }

            var command = new AddTicketCommentCommand
            {
                TicketId = ticketId,
                Content = content.Trim(),
                IsInternal = false
            };

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Comment added successfully with ID: {CommentId} for TicketId: {TicketId}",
                    result.Data, ticketId);
                TempData["Success"] = "Yorum başarıyla eklendi.";
            }
            else
            {
                _logger.LogWarning("Failed to add comment for TicketId {TicketId}: {Errors}",
                    ticketId, string.Join(", ", result.Errors));
                TempData["Error"] = result.Errors.FirstOrDefault() ?? "Yorum eklenirken hata oluştu.";
            }

            return RedirectToAction("Details", new { id = ticketId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AddComment for TicketId: {TicketId}", ticketId);
            TempData["Error"] = "Yorum eklenirken beklenmeyen bir hata oluştu.";
            return RedirectToAction("Details", new { id = ticketId });
        }
    }
}