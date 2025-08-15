using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Infrastructure.Repositories;
using TicketSystem.Domain.Entities;
using TicketSystem.Web.Areas.Admin.Models;
using System.Text.Json;

namespace TicketSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class TicketTypesController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public TicketTypesController(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<IActionResult> Index()
    {
        var ticketTypes = await _unitOfWork.TicketTypes.FindAsync(
            x => x.CompanyId == _currentUserService.CompanyId);
        return View(ticketTypes.OrderBy(x => x.SortOrder));
    }

    public IActionResult Create()
    {
        return View(new CreateTicketTypeViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTicketTypeViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var formDefinition = new
        {
            fields = model.FormFields.Where(f => !string.IsNullOrEmpty(f.Name)).Select(f => new
            {
                name = f.Name,
                label = f.Label,
                type = f.Type,
                required = f.Required,
                options = f.Type == "select" ? f.Options?.Split(',').Select(o => o.Trim()).ToArray() : null,
                placeholder = f.Placeholder,
                validation = new
                {
                    minLength = f.MinLength,
                    maxLength = f.MaxLength,
                    min = f.Min,
                    max = f.Max
                }
            }).ToArray()
        };

        var ticketType = new TicketType
        {
            CompanyId = _currentUserService.CompanyId!.Value,
            Name = model.Name,
            Description = model.Description,
            Icon = model.Icon,
            Color = model.Color,
            FormDefinition = JsonSerializer.Serialize(formDefinition),
            IsActive = true,
            SortOrder = model.DisplayOrder
        };

        await _unitOfWork.TicketTypes.AddAsync(ticketType);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "Ticket türü başarıyla oluşturuldu.";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Edit(int id)
    {
        var ticketType = await _unitOfWork.TicketTypes.GetByIdAsync(id);
        if (ticketType == null || ticketType.CompanyId != _currentUserService.CompanyId)
        {
            return NotFound();
        }

        var model = new EditTicketTypeViewModel
        {
            Id = ticketType.Id,
            Name = ticketType.Name,
            Description = ticketType.Description,
            Icon = ticketType.Icon,
            Color = ticketType.Color,
            DisplayOrder = ticketType.SortOrder,
            IsActive = ticketType.IsActive
        };

        if (!string.IsNullOrEmpty(ticketType.FormDefinition))
        {
            try
            {
                var formDef = JsonSerializer.Deserialize<JsonElement>(ticketType.FormDefinition);
                if (formDef.TryGetProperty("fields", out var fieldsElement))
                {
                    model.FormFields = new List<FormFieldViewModel>();
                    foreach (var field in fieldsElement.EnumerateArray())
                    {
                        var formField = new FormFieldViewModel
                        {
                            Name = field.GetProperty("name").GetString() ?? "",
                            Label = field.GetProperty("label").GetString() ?? "",
                            Type = field.GetProperty("type").GetString() ?? "text",
                            Required = field.TryGetProperty("required", out var req) ? req.GetBoolean() : false,
                            Placeholder = field.TryGetProperty("placeholder", out var ph) ? ph.GetString() : ""
                        };

                        if (field.TryGetProperty("options", out var opts) && opts.ValueKind == JsonValueKind.Array)
                        {
                            formField.Options = string.Join(", ", opts.EnumerateArray().Select(o => o.GetString()));
                        }

                        if (field.TryGetProperty("validation", out var validation))
                        {
                            if (validation.TryGetProperty("minLength", out var minLen)) formField.MinLength = minLen.GetInt32();
                            if (validation.TryGetProperty("maxLength", out var maxLen)) formField.MaxLength = maxLen.GetInt32();
                            if (validation.TryGetProperty("min", out var min)) formField.Min = min.GetInt32();
                            if (validation.TryGetProperty("max", out var max)) formField.Max = max.GetInt32();
                        }

                        model.FormFields.Add(formField);
                    }
                }
            }
            catch
            {
                model.FormFields = new List<FormFieldViewModel>();
            }
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditTicketTypeViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var ticketType = await _unitOfWork.TicketTypes.GetByIdAsync(model.Id);
        if (ticketType == null || ticketType.CompanyId != _currentUserService.CompanyId)
        {
            return NotFound();
        }

        var formDefinition = new
        {
            fields = model.FormFields.Where(f => !string.IsNullOrEmpty(f.Name)).Select(f => new
            {
                name = f.Name,
                label = f.Label,
                type = f.Type,
                required = f.Required,
                options = f.Type == "select" ? f.Options?.Split(',').Select(o => o.Trim()).ToArray() : null,
                placeholder = f.Placeholder,
                validation = new
                {
                    minLength = f.MinLength,
                    maxLength = f.MaxLength,
                    min = f.Min,
                    max = f.Max
                }
            }).ToArray()
        };

        ticketType.Name = model.Name;
        ticketType.Description = model.Description;
        ticketType.Icon = model.Icon;
        ticketType.Color = model.Color;
        ticketType.SortOrder = model.DisplayOrder;
        ticketType.IsActive = model.IsActive;
        ticketType.FormDefinition = JsonSerializer.Serialize(formDefinition);

        _unitOfWork.TicketTypes.Update(ticketType);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "Ticket türü başarıyla güncellendi.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var ticketType = await _unitOfWork.TicketTypes.GetByIdAsync(id);
        if (ticketType == null || ticketType.CompanyId != _currentUserService.CompanyId)
        {
            return NotFound();
        }

        var hasTickets = await _unitOfWork.Tickets.ExistsAsync(t => t.TypeId == id);
        if (hasTickets)
        {
            TempData["Error"] = "Bu ticket türünü kullanan aktif ticket'lar var, silinemez.";
            return RedirectToAction("Index");
        }

        _unitOfWork.TicketTypes.Remove(ticketType);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "Ticket türü başarıyla silindi.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var ticketType = await _unitOfWork.TicketTypes.GetByIdAsync(id);
        if (ticketType == null || ticketType.CompanyId != _currentUserService.CompanyId)
        {
            return NotFound();
        }

        ticketType.IsActive = !ticketType.IsActive;
        _unitOfWork.TicketTypes.Update(ticketType);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = $"Ticket türü {(ticketType.IsActive ? "aktif" : "pasif")} hale getirildi.";
        return RedirectToAction("Index");
    }
}