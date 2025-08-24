using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Domain.Entities;
using TicketSystem.Web.Areas.Admin.Models;

namespace TicketSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class CategoriesController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<CategoriesController> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var categories = await _unitOfWork.TicketCategories.FindAsync(
                x => x.CompanyId == _currentUserService.CompanyId,
                x => x.Modules);
            return View(categories.OrderBy(x => x.SortOrder));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading categories");
            TempData["Error"] = "Kategoriler yüklenirken bir hata oluştu.";
            return View(new List<TicketCategory>());
        }
    }

    public IActionResult Create()
    {
        return View(new CreateCategoryViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateCategoryViewModel model)
    {
        if (!ModelState.IsValid)
        {
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

            // Aynı isimde kategori var mı kontrol et
            var companyCategories = await _unitOfWork.TicketCategories
                .FindAsync(x => x.CompanyId == _currentUserService.CompanyId);

            var nameExists = companyCategories.Any(c =>
                c.Name.ToLower() == model.Name.Trim().ToLower());

            if (nameExists)
            {
                ModelState.AddModelError("Name", "Bu isimde bir kategori zaten var.");
                return View(model);
            }

            var category = new TicketCategory
            {
                CompanyId = _currentUserService.CompanyId.Value,
                Name = model.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
                Icon = model.Icon,
                Color = model.Color,
                IsActive = true,
                SortOrder = model.DisplayOrder,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUserService.UserName ?? "System"
            };

            await _unitOfWork.TicketCategories.AddAsync(category);
            await _unitOfWork.SaveChangesAsync();

            // Modülleri kaydet
            if (model.Modules != null && model.Modules.Any())
            {
                foreach (var moduleModel in model.Modules.Where(m => !string.IsNullOrWhiteSpace(m.Name)))
                {
                    var module = new TicketCategoryModule
                    {
                        TicketCategoryId = category.Id,
                        Name = moduleModel.Name.Trim(),
                        Description = string.IsNullOrWhiteSpace(moduleModel.Description) ? null : moduleModel.Description.Trim(),
                        SortOrder = moduleModel.DisplayOrder,
                        IsActive = moduleModel.IsActive,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = _currentUserService.UserName ?? "System"
                    };
                    await _unitOfWork.TicketCategoryModules.AddAsync(module);
                }
                await _unitOfWork.SaveChangesAsync();
            }

            TempData["Success"] = "Kategori başarıyla oluşturuldu.";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category");
            ModelState.AddModelError("", "Kategori oluşturulurken bir hata oluştu: " + ex.Message);
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var category = await _unitOfWork.TicketCategories.FirstOrDefaultAsync(
                x => x.Id == id && x.CompanyId == _currentUserService.CompanyId,
                x => x.Modules);

            if (category == null)
            {
                TempData["Error"] = "Kategori bulunamadı.";
                return RedirectToAction("Index");
            }

            var model = new EditCategoryViewModel
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Icon = category.Icon,
                Color = category.Color,
                DisplayOrder = category.SortOrder,
                IsActive = category.IsActive,
                Modules = category.Modules.Select(m => new ModuleViewModel
                {
                    Id = m.Id,
                    Name = m.Name,
                    Description = m.Description,
                    DisplayOrder = m.SortOrder,
                    IsActive = m.IsActive
                }).OrderBy(m => m.DisplayOrder).ToList()
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading category for edit: {Id}", id);
            TempData["Error"] = "Kategori yüklenirken bir hata oluştu.";
            return RedirectToAction("Index");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditCategoryViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var category = await _unitOfWork.TicketCategories.FirstOrDefaultAsync(
                x => x.Id == model.Id && x.CompanyId == _currentUserService.CompanyId,
                x => x.Modules);

            if (category == null)
            {
                TempData["Error"] = "Kategori bulunamadı.";
                return RedirectToAction("Index");
            }

            // Aynı isimde başka kategori var mı kontrol et (kendisi hariç)
            var companyCategories = await _unitOfWork.TicketCategories
                .FindAsync(x => x.CompanyId == _currentUserService.CompanyId && x.Id != model.Id);

            var nameExists = companyCategories.Any(c =>
                c.Name.ToLower() == model.Name.Trim().ToLower());

            if (nameExists)
            {
                ModelState.AddModelError("Name", "Bu isimde başka bir kategori var.");
                return View(model);
            }

            // Update category
            category.Name = model.Name.Trim();
            category.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
            category.Icon = model.Icon;
            category.Color = model.Color;
            category.SortOrder = model.DisplayOrder;
            category.IsActive = model.IsActive;
            category.UpdatedAt = DateTime.UtcNow;
            category.UpdatedBy = _currentUserService.UserName ?? "System";

            // Update modules
            // Önce mevcut modülleri sil
            var existingModules = category.Modules.ToList();
            foreach (var module in existingModules)
            {
                _unitOfWork.TicketCategoryModules.Remove(module);
            }

            // Yeni modülleri ekle
            if (model.Modules != null && model.Modules.Any())
            {
                foreach (var moduleModel in model.Modules.Where(m => !string.IsNullOrWhiteSpace(m.Name)))
                {
                    var module = new TicketCategoryModule
                    {
                        TicketCategoryId = category.Id,
                        Name = moduleModel.Name.Trim(),
                        Description = string.IsNullOrWhiteSpace(moduleModel.Description) ? null : moduleModel.Description.Trim(),
                        SortOrder = moduleModel.DisplayOrder,
                        IsActive = moduleModel.IsActive,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = _currentUserService.UserName ?? "System"
                    };
                    await _unitOfWork.TicketCategoryModules.AddAsync(module);
                }
            }

            _unitOfWork.TicketCategories.Update(category);
            await _unitOfWork.SaveChangesAsync();

            TempData["Success"] = "Kategori başarıyla güncellendi.";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category");
            ModelState.AddModelError("", "Kategori güncellenirken bir hata oluştu: " + ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        try
        {
            var category = await _unitOfWork.TicketCategories.FirstOrDefaultAsync(
                x => x.Id == id && x.CompanyId == _currentUserService.CompanyId);

            if (category == null)
            {
                TempData["Error"] = "Kategori bulunamadı.";
                return RedirectToAction("Index");
            }

            category.IsActive = !category.IsActive;
            category.UpdatedAt = DateTime.UtcNow;
            category.UpdatedBy = _currentUserService.UserName ?? "System";

            _unitOfWork.TicketCategories.Update(category);
            await _unitOfWork.SaveChangesAsync();

            TempData["Success"] = $"Kategori {(category.IsActive ? "aktif" : "pasif")} hale getirildi.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling category status");
            TempData["Error"] = "İşlem sırasında bir hata oluştu: " + ex.Message;
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var category = await _unitOfWork.TicketCategories.FirstOrDefaultAsync(
                x => x.Id == id && x.CompanyId == _currentUserService.CompanyId);

            if (category == null)
            {
                TempData["Error"] = "Kategori bulunamadı.";
                return RedirectToAction("Index");
            }

            // Kategoriye bağlı ticketlar var mı kontrol et
            var hasTickets = await _unitOfWork.Tickets.ExistsAsync(t => t.CategoryId == id);
            if (hasTickets)
            {
                TempData["Error"] = "Bu kategoriye ait ticketlar var, silinemez. Önce pasif hale getirin.";
                return RedirectToAction("Index");
            }

            _unitOfWork.TicketCategories.Remove(category);
            await _unitOfWork.SaveChangesAsync();

            TempData["Success"] = "Kategori başarıyla silindi.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category");
            TempData["Error"] = "Kategori silinirken bir hata oluştu: " + ex.Message;
        }

        return RedirectToAction("Index");
    }
}