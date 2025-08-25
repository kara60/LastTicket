using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Application.Common.Interfaces;
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
    private readonly ILogger<TicketTypesController> _logger;

    public TicketTypesController(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<TicketTypesController> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var ticketTypes = await _unitOfWork.TicketTypes.FindAsync(
                x => x.CompanyId == _currentUserService.CompanyId);
            return View(ticketTypes.OrderBy(x => x.SortOrder).ThenBy(x => x.Name));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading ticket types");
            TempData["Error"] = "Ticket türleri yüklenirken bir hata oluştu.";
            return View(new List<TicketType>());
        }
    }

    public IActionResult Create()
    {
        var model = new CreateTicketTypeViewModel();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTicketTypeViewModel model)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Create ticket type form validation failed");
            return View(model);
        }

        try
        {
            if (!_currentUserService.CompanyId.HasValue)
            {
                _logger.LogError("CompanyId is null for current user");
                ModelState.AddModelError("", "Şirket bilgisi alınamadı. Lütfen tekrar giriş yapın.");
                return View(model);
            }

            // Name uniqueness check
            var existingTicketType = await _unitOfWork.TicketTypes.FirstOrDefaultAsync(
                x => x.CompanyId == _currentUserService.CompanyId &&
                     x.Name.ToLower() == model.Name.ToLower().Trim());

            if (existingTicketType != null)
            {
                ModelState.AddModelError("Name", "Bu isimde bir ticket türü zaten mevcut.");
                return View(model);
            }

            // Create form definition
            var formDefinition = new
            {
                fields = model.FormFields?.Where(f => !string.IsNullOrWhiteSpace(f.Name) && !string.IsNullOrWhiteSpace(f.Label))
                    .Select(f => new
                    {
                        name = f.Name.Trim(),
                        label = f.Label.Trim(),
                        type = f.Type ?? "text",
                        required = f.Required,
                        options = f.Type == "select" && !string.IsNullOrEmpty(f.Options)
                            ? f.Options.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                      .Select(o => o.Trim())
                                      .Where(o => !string.IsNullOrEmpty(o))
                                      .ToArray()
                            : null,
                        placeholder = string.IsNullOrWhiteSpace(f.Placeholder) ? null : f.Placeholder.Trim(),
                        validation = new
                        {
                            minLength = f.MinLength > 0 ? f.MinLength : (int?)null,
                            maxLength = f.MaxLength > 0 ? f.MaxLength : (int?)null,
                            min = f.Min.HasValue ? f.Min : (int?)null,
                            max = f.Max.HasValue ? f.Max : (int?)null
                        }
                    }).ToArray() ?? Array.Empty<object>()
            };

            var ticketType = new TicketType
            {
                CompanyId = _currentUserService.CompanyId.Value,
                Name = model.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
                Icon = model.Icon,
                Color = model.Color,
                FormDefinition = JsonSerializer.Serialize(formDefinition, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                }),
                IsActive = true,
                SortOrder = model.DisplayOrder,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUserService.UserName ?? "System"
            };

            await _unitOfWork.TicketTypes.AddAsync(ticketType);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Ticket type created successfully: {Name} (ID: {Id})", ticketType.Name, ticketType.Id);

            TempData["Success"] = "Ticket türü başarıyla oluşturuldu.";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ticket type");
            ModelState.AddModelError("", "Ticket türü oluşturulurken bir hata oluştu: " + ex.Message);
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var ticketType = await _unitOfWork.TicketTypes.FirstOrDefaultAsync(
                x => x.Id == id && x.CompanyId == _currentUserService.CompanyId);

            if (ticketType == null)
            {
                _logger.LogWarning("TicketType not found with ID: {Id}", id);
                TempData["Error"] = "Ticket türü bulunamadı.";
                return RedirectToAction("Index");
            }

            var model = new EditTicketTypeViewModel
            {
                Id = ticketType.Id,
                Name = ticketType.Name,
                Description = ticketType.Description,
                Icon = ticketType.Icon,
                Color = ticketType.Color,
                DisplayOrder = ticketType.SortOrder,
                IsActive = ticketType.IsActive,
                FormFields = new List<FormFieldViewModel>()
            };

            // Parse form definition
            if (!string.IsNullOrEmpty(ticketType.FormDefinition))
            {
                try
                {
                    var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var formDef = JsonSerializer.Deserialize<JsonElement>(ticketType.FormDefinition, jsonOptions);

                    if (formDef.TryGetProperty("fields", out var fieldsElement) && fieldsElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var field in fieldsElement.EnumerateArray())
                        {
                            var formField = new FormFieldViewModel
                            {
                                Name = field.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? "" : "",
                                Label = field.TryGetProperty("label", out var labelEl) ? labelEl.GetString() ?? "" : "",
                                Type = field.TryGetProperty("type", out var typeEl) ? typeEl.GetString() ?? "text" : "text",
                                Required = field.TryGetProperty("required", out var reqEl) && reqEl.ValueKind == JsonValueKind.True,
                                Placeholder = field.TryGetProperty("placeholder", out var phEl) ? phEl.GetString() : ""
                            };

                            // Handle options array
                            if (field.TryGetProperty("options", out var optsEl) && optsEl.ValueKind == JsonValueKind.Array)
                            {
                                var optionsList = new List<string>();
                                foreach (var option in optsEl.EnumerateArray())
                                {
                                    var optStr = option.GetString();
                                    if (!string.IsNullOrEmpty(optStr))
                                    {
                                        optionsList.Add(optStr);
                                    }
                                }
                                formField.Options = string.Join(", ", optionsList);
                            }

                            // Handle validation object
                            if (field.TryGetProperty("validation", out var valEl) && valEl.ValueKind == JsonValueKind.Object)
                            {
                                if (valEl.TryGetProperty("minLength", out var minLenEl) && minLenEl.ValueKind == JsonValueKind.Number)
                                    formField.MinLength = minLenEl.GetInt32();
                                if (valEl.TryGetProperty("maxLength", out var maxLenEl) && maxLenEl.ValueKind == JsonValueKind.Number)
                                    formField.MaxLength = maxLenEl.GetInt32();
                                if (valEl.TryGetProperty("min", out var minEl) && minEl.ValueKind == JsonValueKind.Number)
                                    formField.Min = minEl.GetInt32();
                                if (valEl.TryGetProperty("max", out var maxEl) && maxEl.ValueKind == JsonValueKind.Number)
                                    formField.Max = maxEl.GetInt32();
                            }

                            model.FormFields.Add(formField);
                        }
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Error parsing FormDefinition for TicketType {Id}", id);
                    model.FormFields = new List<FormFieldViewModel>();
                }
            }

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading ticket type for edit: {Id}", id);
            TempData["Error"] = "Ticket türü yüklenirken bir hata oluştu.";
            return RedirectToAction("Index");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditTicketTypeViewModel model)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Edit ticket type form validation failed");
            return View(model);
        }

        try
        {
            var ticketType = await _unitOfWork.TicketTypes.FirstOrDefaultAsync(
                x => x.Id == model.Id && x.CompanyId == _currentUserService.CompanyId);

            if (ticketType == null)
            {
                _logger.LogWarning("TicketType not found for edit: {Id}", model.Id);
                TempData["Error"] = "Ticket türü bulunamadı.";
                return RedirectToAction("Index");
            }

            // Name uniqueness check (exclude current)
            var existingTicketType = await _unitOfWork.TicketTypes.FirstOrDefaultAsync(
                x => x.CompanyId == _currentUserService.CompanyId &&
                     x.Name.ToLower() == model.Name.ToLower().Trim() &&
                     x.Id != model.Id);

            if (existingTicketType != null)
            {
                ModelState.AddModelError("Name", "Bu isimde başka bir ticket türü var.");
                return View(model);
            }

            // Update form definition
            var formDefinition = new
            {
                fields = model.FormFields?.Where(f => !string.IsNullOrWhiteSpace(f.Name) && !string.IsNullOrWhiteSpace(f.Label))
                    .Select(f => new
                    {
                        name = f.Name.Trim(),
                        label = f.Label.Trim(),
                        type = f.Type ?? "text",
                        required = f.Required,
                        options = f.Type == "select" && !string.IsNullOrEmpty(f.Options)
                            ? f.Options.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                      .Select(o => o.Trim())
                                      .Where(o => !string.IsNullOrEmpty(o))
                                      .ToArray()
                            : null,
                        placeholder = string.IsNullOrWhiteSpace(f.Placeholder) ? null : f.Placeholder.Trim(),
                        validation = new
                        {
                            minLength = f.MinLength > 0 ? f.MinLength : (int?)null,
                            maxLength = f.MaxLength > 0 ? f.MaxLength : (int?)null,
                            min = f.Min.HasValue ? f.Min : (int?)null,
                            max = f.Max.HasValue ? f.Max : (int?)null
                        }
                    }).ToArray() ?? Array.Empty<object>()
            };

            // Update ticket type
            ticketType.Name = model.Name.Trim();
            ticketType.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
            ticketType.Icon = model.Icon;
            ticketType.Color = model.Color;
            ticketType.SortOrder = model.DisplayOrder;
            ticketType.IsActive = model.IsActive;
            ticketType.FormDefinition = JsonSerializer.Serialize(formDefinition, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
            ticketType.UpdatedAt = DateTime.UtcNow;
            ticketType.UpdatedBy = _currentUserService.UserName ?? "System";

            _unitOfWork.TicketTypes.Update(ticketType);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Ticket type updated successfully: {Name} (ID: {Id})", ticketType.Name, ticketType.Id);

            TempData["Success"] = "Ticket türü başarıyla güncellendi.";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ticket type: {Id}", model.Id);
            ModelState.AddModelError("", "Ticket türü güncellenirken bir hata oluştu: " + ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var ticketType = await _unitOfWork.TicketTypes.FirstOrDefaultAsync(
                x => x.Id == id && x.CompanyId == _currentUserService.CompanyId);

            if (ticketType == null)
            {
                _logger.LogWarning("TicketType not found for delete: {Id}", id);
                TempData["Error"] = "Ticket türü bulunamadı.";
                return RedirectToAction("Index");
            }

            // Check if any tickets use this type
            var hasTickets = await _unitOfWork.Tickets.ExistsAsync(t => t.TypeId == id);
            if (hasTickets)
            {
                _logger.LogWarning("Cannot delete ticket type {Id} - has associated tickets", id);
                TempData["Error"] = "Bu ticket türünü kullanan ticket'lar bulunduğu için silinemez. Önce ilgili ticket'ları başka türe taşıyın veya silin.";
                return RedirectToAction("Index");
            }

            _unitOfWork.TicketTypes.Remove(ticketType);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Ticket type deleted successfully: {Name} (ID: {Id})", ticketType.Name, ticketType.Id);

            TempData["Success"] = "Ticket türü başarıyla silindi.";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting ticket type: {Id}", id);
            TempData["Error"] = "Ticket türü silinirken bir hata oluştu: " + ex.Message;
            return RedirectToAction("Index");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        try
        {
            var ticketType = await _unitOfWork.TicketTypes.FirstOrDefaultAsync(
                x => x.Id == id && x.CompanyId == _currentUserService.CompanyId);

            if (ticketType == null)
            {
                _logger.LogWarning("TicketType not found for toggle status: {Id}", id);
                TempData["Error"] = "Ticket türü bulunamadı.";
                return RedirectToAction("Index");
            }

            ticketType.IsActive = !ticketType.IsActive;
            ticketType.UpdatedAt = DateTime.UtcNow;
            ticketType.UpdatedBy = _currentUserService.UserName ?? "System";

            _unitOfWork.TicketTypes.Update(ticketType);
            await _unitOfWork.SaveChangesAsync();

            var statusText = ticketType.IsActive ? "aktif" : "pasif";
            _logger.LogInformation("Ticket type status toggled: {Name} (ID: {Id}) is now {Status}",
                ticketType.Name, ticketType.Id, statusText);

            TempData["Success"] = $"Ticket türü '{statusText}' hale getirildi.";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling ticket type status: {Id}", id);
            TempData["Error"] = "Ticket türü durumu değiştirilirken bir hata oluştu: " + ex.Message;
            return RedirectToAction("Index");
        }
    }
}