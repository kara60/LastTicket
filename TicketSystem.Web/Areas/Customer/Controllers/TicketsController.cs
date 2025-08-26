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

    public TicketsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> Index(GetMyTicketsQuery query)
    {
        var result = await _mediator.Send(query);
        return View(result.Data);
    }

    public async Task<IActionResult> Details(int id)
    {
        var result = await _mediator.Send(new GetTicketByIdQuery(id));
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Errors.FirstOrDefault();
            return RedirectToAction("Index");
        }
        return View(result.Data);
    }

    // 4 Adımlı Ticket Oluşturma
    public async Task<IActionResult> Create()
    {
        var ticketTypes = await _mediator.Send(new GetTicketTypesQuery());
        return View(new CreateTicketStep1ViewModel
        {
            TicketTypes = ticketTypes.Data ?? new List<TicketSystem.Application.Features.Common.DTOs.TicketTypeDto>()
        });
    }

    [HttpPost]
    public async Task<IActionResult> SelectType(int typeId)
    {
        Console.WriteLine($"SelectType called with typeId: {typeId}");

        try
        {
            var categories = await _mediator.Send(new GetTicketCategoriesQuery());
            Console.WriteLine($"Categories result isSuccess: {categories.IsSuccess}");
            Console.WriteLine($"Categories count: {categories.Data?.Count ?? 0}");

            if (!categories.IsSuccess)
            {
                Console.WriteLine($"Categories error: {string.Join(", ", categories.Errors)}");
                TempData["Error"] = "Kategoriler yüklenirken hata: " + categories.Errors.FirstOrDefault();
                return RedirectToAction("Create");
            }

            var ticketTypes = await _mediator.Send(new GetTicketTypesQuery());
            Console.WriteLine($"TicketTypes count: {ticketTypes.Data?.Count ?? 0}");

            var selectedType = ticketTypes.Data?.FirstOrDefault(x => x.Id == typeId);
            if (selectedType == null)
            {
                Console.WriteLine($"SelectedType is null for typeId: {typeId}");
                TempData["Error"] = "Geçersiz ticket türü.";
                return RedirectToAction("Create");
            }

            Console.WriteLine($"Returning SelectCategory view for type: {selectedType.Name}");

            var model = new CreateTicketStep2ViewModel
            {
                SelectedTypeId = typeId,
                SelectedType = selectedType,
                Categories = categories.Data ?? new List<TicketCategoryDto>()
            };

            Console.WriteLine($"Model categories count: {model.Categories.Count}");

            return View("SelectCategory", model);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in SelectType: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            TempData["Error"] = "Bir hata oluştu: " + ex.Message;
            return RedirectToAction("Create");
        }
    }

    [HttpPost]
    public async Task<IActionResult> SelectCategory(int typeId, int categoryId)
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

    [HttpPost]
    public async Task<IActionResult> PreviewTicket(CreateTicketStep3ViewModel model)
    {
        // Model binding sorununu çözmek için Request.Form'dan manuel parsing
        if (Request.Form != null)
        {
            // FormData dictionary'yi manuel olarak doldur
            model.FormData = new Dictionary<string, object>();

            foreach (var key in Request.Form.Keys)
            {
                if (key.StartsWith("FormData[") && key.EndsWith("]"))
                {
                    var fieldName = key.Substring(9, key.Length - 10); // "FormData[" ve "]" kısmını çıkar
                    var value = Request.Form[key].ToString();

                    if (!string.IsNullOrEmpty(value))
                    {
                        model.FormData[fieldName] = value;
                    }
                }
            }

            // SelectedModule'u da kontrol et
            if (Request.Form.ContainsKey("SelectedModule"))
            {
                model.SelectedModule = Request.Form["SelectedModule"].ToString();
            }
        }

        // Artık Title required olmadığı için, dinamik formdan title üret
        if (string.IsNullOrWhiteSpace(model.Title))
        {
            model.Title = model.GetDynamicTitle();
        }

        // En az bir form alanının doldurulmuş olup olmadığını kontrol et
        if (model.FormData == null || !model.FormData.Any())
        {
            // Type ve Category bilgilerini tekrar yükle
            var ticketTypesForError = await _mediator.Send(new GetTicketTypesQuery());
            var categoriesForError = await _mediator.Send(new GetTicketCategoriesQuery());

            model.SelectedType = ticketTypesForError.Data?.FirstOrDefault(x => x.Id == model.SelectedTypeId);
            model.SelectedCategory = categoriesForError.Data?.FirstOrDefault(x => x.Id == model.SelectedCategoryId);

            ModelState.AddModelError("", "Lütfen en az bir form alanını doldurunuz.");
            return View("FillForm", model);
        }

        // Gerekli alanlar kontrolü (optional - form definition'da required olan alanlar varsa)
        var ticketTypes = await _mediator.Send(new GetTicketTypesQuery());
        var selectedType = ticketTypes.Data?.FirstOrDefault(x => x.Id == model.SelectedTypeId);

        if (selectedType != null && !string.IsNullOrEmpty(selectedType.FormDefinition))
        {
            try
            {
                var formDef = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(selectedType.FormDefinition);
                if (formDef.TryGetProperty("fields", out var fieldsElement) && fieldsElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var field in fieldsElement.EnumerateArray())
                    {
                        if (field.TryGetProperty("required", out var requiredProp) && requiredProp.GetBoolean() &&
                            field.TryGetProperty("name", out var nameProp))
                        {
                            var fieldName = nameProp.GetString();
                            if (string.IsNullOrEmpty(fieldName) ||
                                !model.FormData.ContainsKey(fieldName) ||
                                string.IsNullOrWhiteSpace(model.FormData[fieldName]?.ToString()))
                            {
                                var labelProp = field.TryGetProperty("label", out var label) ? label.GetString() : fieldName;
                                ModelState.AddModelError("", $"{labelProp} alanı gereklidir.");
                            }
                        }
                    }
                }
            }
            catch (System.Text.Json.JsonException ex)
            {
                
            }
        }

        // Eğer validation hatası varsa form'a geri dön
        if (!ModelState.IsValid)
        {
            var ticketTypesForError = await _mediator.Send(new GetTicketTypesQuery());
            var categoriesForError = await _mediator.Send(new GetTicketCategoriesQuery());

            model.SelectedType = ticketTypesForError.Data?.FirstOrDefault(x => x.Id == model.SelectedTypeId);
            model.SelectedCategory = categoriesForError.Data?.FirstOrDefault(x => x.Id == model.SelectedCategoryId);

            return View("FillForm", model);
        }

        // Önizleme sayfasına yönlendir
        var previewModel = new CreateTicketStep4ViewModel
        {
            SelectedTypeId = model.SelectedTypeId,
            SelectedCategoryId = model.SelectedCategoryId,
            Title = model.Title, // Artık dinamik olarak üretilen title
            Description = model.Description,
            SelectedModule = model.SelectedModule,
            FormData = model.FormData ?? new Dictionary<string, object>()
        };

        // Type ve Category bilgilerini tekrar yükle
        var typesQuery = await _mediator.Send(new GetTicketTypesQuery());
        var categoriesQuery = await _mediator.Send(new GetTicketCategoriesQuery());

        previewModel.SelectedType = typesQuery.Data?.FirstOrDefault(x => x.Id == model.SelectedTypeId);
        previewModel.SelectedCategory = categoriesQuery.Data?.FirstOrDefault(x => x.Id == model.SelectedCategoryId);

        return View("Preview", previewModel);
    }

    [HttpPost]
    public async Task<IActionResult> SubmitTicket(CreateTicketStep4ViewModel model)
    {
        var command = new CreateTicketCommand
        {
            TypeId = model.SelectedTypeId,
            CategoryId = model.SelectedCategoryId,
            Title = model.Title,
            Description = model.Description,
            SelectedModule = model.SelectedModule,
            FormData = model.FormData
        };

        var result = await _mediator.Send(command);

        if (result.IsSuccess)
        {
            TempData["Success"] = $"Ticket başarıyla oluşturuldu. Ticket No: {result.Data}";
            return RedirectToAction("Index");
        }

        TempData["Error"] = result.Errors.FirstOrDefault();
        return RedirectToAction("Create");
    }

    [HttpPost]
    public async Task<IActionResult> AddComment(int ticketId, string content)
    {
        var command = new AddTicketCommentCommand
        {
            TicketId = ticketId,
            Content = content,
            IsInternal = false
        };

        var result = await _mediator.Send(command);

        if (result.IsSuccess)
        {
            TempData["Success"] = "Yorum başarıyla eklendi.";
        }
        else
        {
            TempData["Error"] = result.Errors.FirstOrDefault();
        }

        return RedirectToAction("Details", new { id = ticketId });
    }
}