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

            // ENHANCED Form data debug
            _logger.LogInformation("=== FORM DATA DEBUG START ===");
            foreach (var key in Request.Form.Keys)
            {
                var value = Request.Form[key];
                _logger.LogInformation("Form[{Key}] = '{Value}'", key, value);
            }
            _logger.LogInformation("=== FORM DATA DEBUG END ===");

            // Fix IDs from form if needed
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

            // ENHANCED FormData parsing with detailed logging
            model.FormData = new Dictionary<string, object>();
            var formDataCount = 0;

            foreach (var key in Request.Form.Keys)
            {
                if (key.StartsWith("FormData[") && key.EndsWith("]"))
                {
                    var fieldName = key.Substring(9, key.Length - 10);
                    var value = Request.Form[key].ToString().Trim();

                    _logger.LogInformation("Processing FormData field: '{FieldName}' = '{Value}'", fieldName, value);

                    if (!string.IsNullOrEmpty(value))
                    {
                        model.FormData[fieldName] = value;
                        formDataCount++;
                        _logger.LogInformation("Added to FormData: '{FieldName}' = '{Value}'", fieldName, value);
                    }
                    else
                    {
                        _logger.LogWarning("Skipped empty FormData field: '{FieldName}'", fieldName);
                    }
                }
            }

            _logger.LogInformation("Total FormData fields parsed: {Count}", formDataCount);

            if (Request.Form.ContainsKey("SelectedModule"))
            {
                model.SelectedModule = Request.Form["SelectedModule"].ToString();
                _logger.LogInformation("SelectedModule: '{SelectedModule}'", model.SelectedModule);
            }

            // Load Type and Category
            var ticketTypes = await _mediator.Send(new GetTicketTypesQuery());
            var categories = await _mediator.Send(new GetTicketCategoriesQuery());

            model.SelectedType = ticketTypes.Data?.FirstOrDefault(x => x.Id == model.SelectedTypeId);
            model.SelectedCategory = categories.Data?.FirstOrDefault(x => x.Id == model.SelectedCategoryId);

            _logger.LogInformation("Loaded Type: {TypeName}, Category: {CategoryName}",
                model.SelectedType?.Name ?? "NULL", model.SelectedCategory?.Name ?? "NULL");

            // Generate title if empty
            if (string.IsNullOrWhiteSpace(model.Title))
            {
                model.Title = model.GetDynamicTitle();
                _logger.LogInformation("Generated dynamic title: '{Title}'", model.Title);
            }

            var previewModel = new CreateTicketStep4ViewModel
            {
                SelectedTypeId = model.SelectedTypeId,
                SelectedCategoryId = model.SelectedCategoryId,
                Title = model.Title,
                Description = model.Description,
                SelectedModule = model.SelectedModule,
                FormData = model.FormData, // Bu Dictionary burada doğru olmalı
                SelectedType = model.SelectedType,
                SelectedCategory = model.SelectedCategory
            };

            _logger.LogInformation("PreviewModel created with {FormDataCount} form fields", previewModel.FormData.Count);

            // FormData içeriğini de logla
            foreach (var kvp in previewModel.FormData)
            {
                _logger.LogInformation("PreviewModel FormData[{Key}] = '{Value}'", kvp.Key, kvp.Value);
            }

            return View("Preview", previewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in PreviewTicket");
            TempData["Error"] = "Önizleme oluşturulurken bir hata oluştu.";
            return RedirectToAction("Create");
        }
    }

    [HttpPost]
    public async Task<IActionResult> SubmitTicket(CreateTicketStep4ViewModel model)
    {
        try
        {
            // Generate title if empty
            if (string.IsNullOrWhiteSpace(model.Title))
            {
                var ticketTypes = await _mediator.Send(new GetTicketTypesQuery());
                var categories = await _mediator.Send(new GetTicketCategoriesQuery());

                var selectedType = ticketTypes.Data?.FirstOrDefault(x => x.Id == model.SelectedTypeId);
                var selectedCategory = categories.Data?.FirstOrDefault(x => x.Id == model.SelectedCategoryId);

                // Dynamic title generation
                var titleKeys = new[] { "title", "baslik", "name", "ad", "konu" };
                foreach (var key in titleKeys)
                {
                    if (model.FormData.ContainsKey(key) && !string.IsNullOrWhiteSpace(model.FormData[key]?.ToString()))
                    {
                        model.Title = model.FormData[key].ToString()!;
                        break;
                    }
                }

                if (string.IsNullOrWhiteSpace(model.Title))
                {
                    model.Title = $"{selectedType?.Name} - {selectedCategory?.Name} - {DateTime.Now:dd.MM.yyyy HH:mm}";
                }
            }

            var command = new CreateTicketCommand
            {
                TypeId = model.SelectedTypeId,
                CategoryId = model.SelectedCategoryId,
                Title = model.Title,
                Description = model.Description,
                SelectedModule = model.SelectedModule,
                FormData = model.FormData // Dictionary burada Command'a geçiriliyor
            };

            foreach (var kvp in command.FormData)
            {
                _logger.LogInformation("Command FormData[{Key}] = '{Value}' (Type: {Type})",
                    kvp.Key, kvp.Value, kvp.Value?.GetType().Name ?? "null");
            }

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