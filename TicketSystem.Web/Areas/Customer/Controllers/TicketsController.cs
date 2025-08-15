using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Application.Features.Tickets.Commands.CreateTicket;
using TicketSystem.Application.Features.Tickets.Commands.AddComment;
using TicketSystem.Application.Features.Tickets.Queries.GetMyTickets;
using TicketSystem.Application.Features.Tickets.Queries.GetTicketById;
using TicketSystem.Application.Features.TicketTypes.Queries.GetTicketTypes;
using TicketSystem.Application.Features.TicketCategories.Queries.GetTicketCategories;
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

    public async Task<IActionResult> Details(Guid id)
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
    public async Task<IActionResult> SelectType(Guid typeId)
    {
        var categories = await _mediator.Send(new GetTicketCategoriesQuery());
        var ticketTypes = await _mediator.Send(new GetTicketTypesQuery());

        var selectedType = ticketTypes.Data?.FirstOrDefault(x => x.Id == typeId);
        if (selectedType == null)
        {
            TempData["Error"] = "Geçersiz ticket türü.";
            return RedirectToAction("Create");
        }

        return View("SelectCategory", new CreateTicketStep2ViewModel
        {
            SelectedTypeId = typeId,
            SelectedType = selectedType,
            Categories = categories.Data ?? new List<TicketSystem.Application.Features.Common.DTOs.TicketCategoryDto>()
        });
    }

    [HttpPost]
    public async Task<IActionResult> SelectCategory(Guid typeId, Guid categoryId)
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
        if (!ModelState.IsValid)
        {
            var ticketTypes = await _mediator.Send(new GetTicketTypesQuery());
            var categories = await _mediator.Send(new GetTicketCategoriesQuery());

            model.SelectedType = ticketTypes.Data?.FirstOrDefault(x => x.Id == model.SelectedTypeId);
            model.SelectedCategory = categories.Data?.FirstOrDefault(x => x.Id == model.SelectedCategoryId);

            return View("FillForm", model);
        }

        return View("Preview", new CreateTicketStep4ViewModel
        {
            SelectedTypeId = model.SelectedTypeId,
            SelectedCategoryId = model.SelectedCategoryId,
            Title = model.Title,
            Description = model.Description,
            SelectedModule = model.SelectedModule,
            FormData = model.FormData ?? new Dictionary<string, object>(),
            SelectedType = model.SelectedType,
            SelectedCategory = model.SelectedCategory
        });
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
    public async Task<IActionResult> AddComment(Guid ticketId, string content)
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