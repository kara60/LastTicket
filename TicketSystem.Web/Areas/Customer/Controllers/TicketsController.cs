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
        Console.WriteLine("=== PreviewTicket START DEBUG ===");
        Console.WriteLine($"RECEIVED - SelectedTypeId: {model.SelectedTypeId}");
        Console.WriteLine($"RECEIVED - SelectedCategoryId: {model.SelectedCategoryId}");
        Console.WriteLine($"RECEIVED - Title: '{model.Title}'");

        // Request.Form debug - gerçekte ne geliyor?
        foreach (var key in Request.Form.Keys)
        {
            Console.WriteLine($"Form[{key}] = {Request.Form[key]}");
        }

        // CRITICAL: Eğer 0 geliyorsa Request.Form'dan manuel oku
        if (model.SelectedTypeId == 0)
        {
            var typeIdStr = Request.Form["SelectedTypeId"].ToString();
            if (int.TryParse(typeIdStr, out int typeId))
            {
                model.SelectedTypeId = typeId;
                Console.WriteLine($"FIXED SelectedTypeId from Form: {typeId}");
            }
        }

        if (model.SelectedCategoryId == 0)
        {
            var categoryIdStr = Request.Form["SelectedCategoryId"].ToString();
            if (int.TryParse(categoryIdStr, out int categoryId))
            {
                model.SelectedCategoryId = categoryId;
                Console.WriteLine($"FIXED SelectedCategoryId from Form: {categoryId}");
            }
        }

        Console.WriteLine($"AFTER FIX - SelectedTypeId: {model.SelectedTypeId}");
        Console.WriteLine($"AFTER FIX - SelectedCategoryId: {model.SelectedCategoryId}");

        // FormData parsing
        model.FormData = new Dictionary<string, object>();
        foreach (var key in Request.Form.Keys)
        {
            if (key.StartsWith("FormData[") && key.EndsWith("]"))
            {
                var fieldName = key.Substring(9, key.Length - 10);
                var value = Request.Form[key].ToString();
                if (!string.IsNullOrEmpty(value))
                {
                    model.FormData[fieldName] = value;
                }
            }
        }

        if (Request.Form.ContainsKey("SelectedModule"))
        {
            model.SelectedModule = Request.Form["SelectedModule"].ToString();
        }

        // Type ve Category yükle
        var ticketTypes = await _mediator.Send(new GetTicketTypesQuery());
        var categories = await _mediator.Send(new GetTicketCategoriesQuery());

        model.SelectedType = ticketTypes.Data?.FirstOrDefault(x => x.Id == model.SelectedTypeId);
        model.SelectedCategory = categories.Data?.FirstOrDefault(x => x.Id == model.SelectedCategoryId);

        Console.WriteLine($"LOADED - SelectedType: {model.SelectedType?.Name ?? "NULL"}");
        Console.WriteLine($"LOADED - SelectedCategory: {model.SelectedCategory?.Name ?? "NULL"}");

        // Title generate
        if (string.IsNullOrWhiteSpace(model.Title))
        {
            model.Title = model.GetDynamicTitle();
        }

        // Validation skip for now - focus on data flow
        Console.WriteLine($"FormData count: {model.FormData.Count}");

        var previewModel = new CreateTicketStep4ViewModel
        {
            SelectedTypeId = model.SelectedTypeId,
            SelectedCategoryId = model.SelectedCategoryId,
            Title = model.Title,
            Description = model.Description,
            SelectedModule = model.SelectedModule,
            FormData = model.FormData,
            SelectedType = model.SelectedType,
            SelectedCategory = model.SelectedCategory
        };

        Console.WriteLine($"PREVIEW MODEL - TypeId: {previewModel.SelectedTypeId}, CategoryId: {previewModel.SelectedCategoryId}");
        Console.WriteLine($"PREVIEW MODEL - Type: {previewModel.SelectedType?.Name}, Category: {previewModel.SelectedCategory?.Name}");

        return View("Preview", previewModel);
    }

    [HttpPost]
    public async Task<IActionResult> SubmitTicket(CreateTicketStep4ViewModel model)
    {
        Console.WriteLine("=== SubmitTicket Debug ===");
        Console.WriteLine($"Title: '{model.Title}'");
        Console.WriteLine($"TypeId: {model.SelectedTypeId}");
        Console.WriteLine($"CategoryId: {model.SelectedCategoryId}");
        Console.WriteLine($"FormData count: {model.FormData.Count}");

        // Title boşsa dinamik oluştur
        if (string.IsNullOrWhiteSpace(model.Title))
        {
            // Type ve Category bilgilerini yükle
            var ticketTypes = await _mediator.Send(new GetTicketTypesQuery());
            var categories = await _mediator.Send(new GetTicketCategoriesQuery());

            var selectedType = ticketTypes.Data?.FirstOrDefault(x => x.Id == model.SelectedTypeId);
            var selectedCategory = categories.Data?.FirstOrDefault(x => x.Id == model.SelectedCategoryId);

            // Title'ı dinamik oluştur
            var titleKeys = new[] { "title", "baslik", "name", "ad", "konu" };

            foreach (var key in titleKeys)
            {
                if (model.FormData.ContainsKey(key) && !string.IsNullOrWhiteSpace(model.FormData[key]?.ToString()))
                {
                    model.Title = model.FormData[key].ToString()!;
                    break;
                }
            }

            // Eğer hala boşsa otomatik oluştur
            if (string.IsNullOrWhiteSpace(model.Title))
            {
                model.Title = $"{selectedType?.Name} - {selectedCategory?.Name} - {DateTime.Now:dd.MM.yyyy HH:mm}";
            }

            Console.WriteLine($"Generated title for submit: '{model.Title}'");
        }

        var command = new CreateTicketCommand
        {
            TypeId = model.SelectedTypeId,
            CategoryId = model.SelectedCategoryId,
            Title = model.Title, // Artık boş olmayacak
            Description = model.Description,
            SelectedModule = model.SelectedModule,
            FormData = model.FormData
        };

        Console.WriteLine($"Command Title: '{command.Title}'");

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