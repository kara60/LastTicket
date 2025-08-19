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
            return View(ticketTypes.OrderBy(x => x.SortOrder));
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
    public async Task<IActionResult> Create(CreateTicketTypeViewModel model, string FormFieldsJson)
    {
        _logger.LogInformation("=== CREATE TICKET TYPE POST DEBUG ===");

        // DEBUG: Raw request form data
        _logger.LogInformation("=== RAW FORM DATA ===");
        foreach (var key in Request.Form.Keys)
        {
            _logger.LogInformation("Form Key: {Key} = {Value}", key, Request.Form[key]);
        }

        _logger.LogInformation("=== MODEL VALUES ===");
        _logger.LogInformation("Name: '{Name}'", model?.Name ?? "NULL");
        _logger.LogInformation("Description: '{Description}'", model?.Description ?? "NULL");
        _logger.LogInformation("Icon: '{Icon}'", model?.Icon ?? "NULL");
        _logger.LogInformation("Color: '{Color}'", model?.Color ?? "NULL");
        _logger.LogInformation("DisplayOrder: {DisplayOrder}", model?.DisplayOrder ?? 0);

        _logger.LogInformation("=== FORM FIELDS JSON ===");
        _logger.LogInformation("FormFieldsJson: '{Json}'", FormFieldsJson ?? "NULL");

        // Parse FormFields from JSON
        if (!string.IsNullOrEmpty(FormFieldsJson))
        {
            try
            {
                var formFields = JsonSerializer.Deserialize<List<FormFieldViewModel>>(FormFieldsJson);
                if (model != null)
                {
                    model.FormFields = formFields ?? new List<FormFieldViewModel>();
                    _logger.LogInformation("Parsed FormFields. Count: {Count}", model.FormFields.Count);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing FormFields JSON");
                if (model != null)
                {
                    model.FormFields = new List<FormFieldViewModel>();
                }
            }
        }
        else
        {
            if (model != null)
            {
                model.FormFields = new List<FormFieldViewModel>();
            }
        }

        // Check if model is null
        if (model == null)
        {
            _logger.LogError("Model is NULL!");
            return View(new CreateTicketTypeViewModel());
        }

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("ModelState is invalid");
            foreach (var state in ModelState)
            {
                foreach (var error in state.Value.Errors)
                {
                    _logger.LogWarning("Model Error - {Key}: {ErrorMessage}", state.Key, error.ErrorMessage);
                }
            }
            return View(model);
        }

        try
        {
            // CompanyId kontrolü
            if (!_currentUserService.CompanyId.HasValue)
            {
                _logger.LogError("CompanyId is null for current user");
                ModelState.AddModelError("", "Şirket bilgisi alınamadı. Lütfen tekrar giriş yapın.");
                return View(model);
            }

            // Aynı isimde ticket type var mı kontrol et
            var existingTicketType = await _unitOfWork.TicketTypes.FirstOrDefaultAsync(
                x => x.CompanyId == _currentUserService.CompanyId &&
                     x.Name.ToLower() == model.Name.ToLower().Trim());

            if (existingTicketType != null)
            {
                ModelState.AddModelError("Name", "Bu isimde bir ticket türü zaten mevcut.");
                return View(model);
            }

            // Form definition oluştur
            var formDefinition = new
            {
                fields = model.FormFields?.Where(f => !string.IsNullOrEmpty(f.Name) && !string.IsNullOrEmpty(f.Label))
                    .Select(f => new
                    {
                        name = f.Name.Trim(),
                        label = f.Label.Trim(),
                        type = f.Type ?? "text",
                        required = f.Required,
                        options = f.Type == "select" && !string.IsNullOrEmpty(f.Options)
                            ? f.Options.Split(',').Select(o => o.Trim()).Where(o => !string.IsNullOrEmpty(o)).ToArray()
                            : null,
                        placeholder = !string.IsNullOrEmpty(f.Placeholder) ? f.Placeholder.Trim() : null,
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
                Description = !string.IsNullOrEmpty(model.Description) ? model.Description.Trim() : null,
                Icon = model.Icon,
                Color = model.Color,
                FormDefinition = JsonSerializer.Serialize(formDefinition, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }),
                IsActive = true,
                SortOrder = model.DisplayOrder,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUserService.UserId?.ToString()
            };

            _logger.LogInformation("TicketType object created successfully");

            await _unitOfWork.TicketTypes.AddAsync(ticketType);
            var saveResult = await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("TicketType saved to database. SaveChanges result: {SaveResult}", saveResult);

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
            _logger.LogInformation("=== EDIT TICKET TYPE GET === ID: {Id}", id);

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

            // FormDefinition'ı parse et
            if (!string.IsNullOrEmpty(ticketType.FormDefinition))
            {
                try
                {
                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

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
                                Required = field.TryGetProperty("required", out var req) && req.ValueKind == JsonValueKind.True,
                                Placeholder = field.TryGetProperty("placeholder", out var ph) ? ph.GetString() : ""
                            };

                            // Options array'ini handle et
                            if (field.TryGetProperty("options", out var opts) && opts.ValueKind == JsonValueKind.Array)
                            {
                                var optionsList = new List<string>();
                                foreach (var option in opts.EnumerateArray())
                                {
                                    var optStr = option.GetString();
                                    if (!string.IsNullOrEmpty(optStr))
                                    {
                                        optionsList.Add(optStr);
                                    }
                                }
                                formField.Options = string.Join(", ", optionsList);
                            }

                            // Validation object'ini handle et
                            if (field.TryGetProperty("validation", out var validation) && validation.ValueKind == JsonValueKind.Object)
                            {
                                if (validation.TryGetProperty("minLength", out var minLen) && minLen.ValueKind == JsonValueKind.Number)
                                {
                                    formField.MinLength = minLen.GetInt32();
                                }
                                if (validation.TryGetProperty("maxLength", out var maxLen) && maxLen.ValueKind == JsonValueKind.Number)
                                {
                                    formField.MaxLength = maxLen.GetInt32();
                                }
                                if (validation.TryGetProperty("min", out var min) && min.ValueKind == JsonValueKind.Number)
                                {
                                    formField.Min = min.GetInt32();
                                }
                                if (validation.TryGetProperty("max", out var max) && max.ValueKind == JsonValueKind.Number)
                                {
                                    formField.Max = max.GetInt32();
                                }
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

            _logger.LogInformation("Edit model created for ticket type: {Name}", ticketType.Name);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ticket type for edit with ID: {Id}", id);
            TempData["Error"] = "Ticket türü yüklenirken bir hata oluştu.";
            return RedirectToAction("Index");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditTicketTypeViewModel model)
    {
        _logger.LogInformation("=== EDIT TICKET TYPE POST === ID: {Id}", model?.Id);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("ModelState is invalid for ticket type edit");
            foreach (var state in ModelState)
            {
                foreach (var error in state.Value.Errors)
                {
                    _logger.LogWarning("Model Error - {Key}: {ErrorMessage}", state.Key, error.ErrorMessage);
                }
            }
            return View(model);
        }

        try
        {
            var ticketType = await _unitOfWork.TicketTypes.FirstOrDefaultAsync(
                x => x.Id == model.Id && x.CompanyId == _currentUserService.CompanyId);

            if (ticketType == null)
            {
                _logger.LogWarning("TicketType not found for edit with ID: {Id}", model.Id);
                TempData["Error"] = "Ticket türü bulunamadı.";
                return RedirectToAction("Index");
            }

            // Aynı isimde başka ticket type var mı kontrol et (mevcut hariç)
            var existingTicketType = await _unitOfWork.TicketTypes.FirstOrDefaultAsync(
                x => x.CompanyId == _currentUserService.CompanyId &&
                     x.Name.ToLower() == model.Name.ToLower().Trim() &&
                     x.Id != model.Id);

            if (existingTicketType != null)
            {
                ModelState.AddModelError("Name", "Bu isimde bir ticket türü zaten mevcut.");
                return View(model);
            }

            // Form definition güncelle
            var formDefinition = new
            {
                fields = model.FormFields?.Where(f => !string.IsNullOrEmpty(f.Name) && !string.IsNullOrEmpty(f.Label))
                    .Select(f => new
                    {
                        name = f.Name.Trim(),
                        label = f.Label.Trim(),
                        type = f.Type ?? "text",
                        required = f.Required,
                        options = f.Type == "select" && !string.IsNullOrEmpty(f.Options)
                            ? f.Options.Split(',').Select(o => o.Trim()).Where(o => !string.IsNullOrEmpty(o)).ToArray()
                            : null,
                        placeholder = !string.IsNullOrEmpty(f.Placeholder) ? f.Placeholder.Trim() : null,
                        validation = new
                        {
                            minLength = f.MinLength > 0 ? f.MinLength : (int?)null,
                            maxLength = f.MaxLength > 0 ? f.MaxLength : (int?)null,
                            min = f.Min.HasValue ? f.Min : (int?)null,
                            max = f.Max.HasValue ? f.Max : (int?)null
                        }
                    }).ToArray() ?? Array.Empty<object>()
            };

            // TicketType bilgilerini güncelle
            ticketType.Name = model.Name.Trim();
            ticketType.Description = !string.IsNullOrEmpty(model.Description) ? model.Description.Trim() : null;
            ticketType.Icon = model.Icon;
            ticketType.Color = model.Color;
            ticketType.SortOrder = model.DisplayOrder;
            ticketType.IsActive = model.IsActive;
            ticketType.FormDefinition = JsonSerializer.Serialize(formDefinition, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            ticketType.UpdatedAt = DateTime.UtcNow;
            ticketType.UpdatedBy = _currentUserService.UserId?.ToString();

            _unitOfWork.TicketTypes.Update(ticketType);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("TicketType updated successfully: {Name}", ticketType.Name);

            TempData["Success"] = "Ticket türü başarıyla güncellendi.";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ticket type with ID: {Id}", model.Id);
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
            _logger.LogInformation("=== DELETE TICKET TYPE === ID: {Id}", id);

            var ticketType = await _unitOfWork.TicketTypes.FirstOrDefaultAsync(
                x => x.Id == id && x.CompanyId == _currentUserService.CompanyId);

            if (ticketType == null)
            {
                _logger.LogWarning("TicketType not found for delete with ID: {Id}", id);
                TempData["Error"] = "Ticket türü bulunamadı.";
                return RedirectToAction("Index");
            }

            // Bu türde ticket var mı kontrol et
            var hasTickets = await _unitOfWork.Tickets.ExistsAsync(t => t.TypeId == id);
            if (hasTickets)
            {
                TempData["Error"] = "Bu ticket türünü kullanan ticket'lar bulunduğu için silinemez. Önce ticket'ları başka türe taşıyın veya silin.";
                return RedirectToAction("Index");
            }

            //_unitOfWork.TicketTypes.Delete(ticketType);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("TicketType deleted successfully: {Name}", ticketType.Name);

            TempData["Success"] = "Ticket türü başarıyla silindi.";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting ticket type with ID: {Id}", id);
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
            _logger.LogInformation("=== TOGGLE TICKET TYPE STATUS === ID: {Id}", id);

            var ticketType = await _unitOfWork.TicketTypes.FirstOrDefaultAsync(
                x => x.Id == id && x.CompanyId == _currentUserService.CompanyId);

            if (ticketType == null)
            {
                _logger.LogWarning("TicketType not found for toggle status with ID: {Id}", id);
                TempData["Error"] = "Ticket türü bulunamadı.";
                return RedirectToAction("Index");
            }

            ticketType.IsActive = !ticketType.IsActive;
            ticketType.UpdatedAt = DateTime.UtcNow;
            ticketType.UpdatedBy = _currentUserService.UserId?.ToString();

            _unitOfWork.TicketTypes.Update(ticketType);
            await _unitOfWork.SaveChangesAsync();

            var statusText = ticketType.IsActive ? "aktif" : "pasif";
            _logger.LogInformation("TicketType status toggled: {Name} is now {Status}", ticketType.Name, statusText);

            TempData["Success"] = $"Ticket türü durumu '{statusText}' olarak güncellendi.";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling ticket type status with ID: {Id}", id);
            TempData["Error"] = "Ticket türü durumu değiştirilirken bir hata oluştu.";
            return RedirectToAction("Index");
        }
    }
}