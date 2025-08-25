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

            return View(categories.OrderBy(x => x.SortOrder).ThenBy(x => x.Name));
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
        var model = new CreateCategoryViewModel
        {
            Icon = "folder",
            Color = "#6366f1",
            DisplayOrder = 0,
            Modules = new List<ModuleViewModel>()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateCategoryViewModel model)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Create category form validation failed. Errors: {Errors}",
                string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
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

            // Icon ve Color validation
            if (string.IsNullOrWhiteSpace(model.Icon))
            {
                ModelState.AddModelError("Icon", "Lütfen bir simge seçin.");
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.Color) || !System.Text.RegularExpressions.Regex.IsMatch(model.Color, @"^#[0-9A-Fa-f]{6}$"))
            {
                ModelState.AddModelError("Color", "Lütfen geçerli bir renk kodu girin (#RRGGBB formatında).");
                return View(model);
            }

            // Aynı isimde kategori var mı kontrol et
            var companyCategories = await _unitOfWork.TicketCategories
                .FindAsync(x => x.CompanyId == _currentUserService.CompanyId);

            var nameExists = companyCategories.Any(c =>
                c.Name.ToLower().Trim() == model.Name.Trim().ToLower());

            if (nameExists)
            {
                ModelState.AddModelError("Name", "Bu isimde bir kategori zaten var.");
                return View(model);
            }

            // Kategori oluştur
            var category = new TicketCategory
            {
                CompanyId = _currentUserService.CompanyId.Value,
                Name = model.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
                Icon = model.Icon.Trim(),
                Color = model.Color.Trim().ToUpper(),
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

            _logger.LogInformation("Category created successfully. ID: {CategoryId}, Name: {CategoryName}",
                category.Id, category.Name);

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
                _logger.LogWarning("Category not found or access denied. ID: {CategoryId}", id);
                TempData["Error"] = "Kategori bulunamadı veya erişim yetkiniz yok.";
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
                }).OrderBy(m => m.DisplayOrder).ThenBy(m => m.Name).ToList()
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
    public async Task<IActionResult> Edit(int id, EditCategoryViewModel model)
    {
        if (id != model.Id)
        {
            _logger.LogWarning("Edit category ID mismatch. Route ID: {RouteId}, Model ID: {ModelId}", id, model.Id);
            TempData["Error"] = "Geçersiz istek.";
            return RedirectToAction("Index");
        }

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Edit category form validation failed. Errors: {Errors}",
                string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return View(model);
        }

        try
        {
            var category = await _unitOfWork.TicketCategories.FirstOrDefaultAsync(
                x => x.Id == model.Id && x.CompanyId == _currentUserService.CompanyId,
                x => x.Modules);

            if (category == null)
            {
                _logger.LogWarning("Category not found for edit or access denied. ID: {CategoryId}", model.Id);
                TempData["Error"] = "Kategori bulunamadı veya erişim yetkiniz yok.";
                return RedirectToAction("Index");
            }

            // Icon ve Color validation
            if (string.IsNullOrWhiteSpace(model.Icon))
            {
                ModelState.AddModelError("Icon", "Lütfen bir simge seçin.");
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.Color) || !System.Text.RegularExpressions.Regex.IsMatch(model.Color, @"^#[0-9A-Fa-f]{6}$"))
            {
                ModelState.AddModelError("Color", "Lütfen geçerli bir renk kodu girin (#RRGGBB formatında).");
                return View(model);
            }

            // Aynı isimde başka kategori var mı kontrol et (kendisi hariç)
            var companyCategories = await _unitOfWork.TicketCategories
                .FindAsync(x => x.CompanyId == _currentUserService.CompanyId && x.Id != model.Id);

            var nameExists = companyCategories.Any(c =>
                c.Name.ToLower().Trim() == model.Name.Trim().ToLower());

            if (nameExists)
            {
                ModelState.AddModelError("Name", "Bu isimde başka bir kategori var.");
                return View(model);
            }

            // Update category
            category.Name = model.Name.Trim();
            category.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
            category.Icon = model.Icon.Trim();
            category.Color = model.Color.Trim().ToUpper();
            category.SortOrder = model.DisplayOrder;
            category.IsActive = model.IsActive;
            category.UpdatedAt = DateTime.UtcNow;
            category.UpdatedBy = _currentUserService.UserName ?? "System";

            // Update modules - Daha güvenli bir yaklaşım
            var existingModules = category.Modules.ToList();

            // Önce mevcut modülleri clear et
            category.Modules.Clear();

            // Veritabanından manuel sil
            foreach (var module in existingModules)
            {
                _unitOfWork.TicketCategoryModules.Remove(module);
            }

            await _unitOfWork.SaveChangesAsync(); // Önce silme işlemini commit et

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
                    category.Modules.Add(module); // Navigation property'ye de ekle
                }
            }

            _unitOfWork.TicketCategories.Update(category);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Category updated successfully. ID: {CategoryId}, Name: {CategoryName}",
                category.Id, category.Name);

            TempData["Success"] = "Kategori başarıyla güncellendi.";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category: {CategoryId}", model.Id);
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
                _logger.LogWarning("Category not found for toggle status or access denied. ID: {CategoryId}", id);
                TempData["Error"] = "Kategori bulunamadı veya erişim yetkiniz yok.";
                return RedirectToAction("Index");
            }

            category.IsActive = !category.IsActive;
            category.UpdatedAt = DateTime.UtcNow;
            category.UpdatedBy = _currentUserService.UserName ?? "System";

            _unitOfWork.TicketCategories.Update(category);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Category status toggled. ID: {CategoryId}, IsActive: {IsActive}",
                category.Id, category.IsActive);

            TempData["Success"] = $"Kategori {(category.IsActive ? "aktif" : "pasif")} hale getirildi.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling category status: {CategoryId}", id);
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
                x => x.Id == id && x.CompanyId == _currentUserService.CompanyId,
                x => x.Modules);

            if (category == null)
            {
                _logger.LogWarning("Category not found for delete or access denied. ID: {CategoryId}", id);
                TempData["Error"] = "Kategori bulunamadı veya erişim yetkiniz yok.";
                return RedirectToAction("Index");
            }

            // Kategoriye bağlı ticketlar var mı kontrol et
            var hasTickets = await _unitOfWork.Tickets.ExistsAsync(t => t.CategoryId == id);
            if (hasTickets)
            {
                _logger.LogWarning("Cannot delete category with associated tickets. ID: {CategoryId}", id);
                TempData["Error"] = "Bu kategoriye ait ticketlar var, silinemez. Önce kategoriyi pasif hale getirin.";
                return RedirectToAction("Index");
            }

            // Önce modülleri sil
            var modules = category.Modules.ToList();
            foreach (var module in modules)
            {
                _unitOfWork.TicketCategoryModules.Remove(module);
            }

            // Sonra kategoriyi sil
            _unitOfWork.TicketCategories.Remove(category);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Category deleted successfully. ID: {CategoryId}, Name: {CategoryName}",
                category.Id, category.Name);

            TempData["Success"] = "Kategori ve tüm modülleri başarıyla silindi.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category: {CategoryId}", id);
            TempData["Error"] = "Kategori silinirken bir hata oluştu: " + ex.Message;
        }

        return RedirectToAction("Index");
    }

    // API endpoint for getting category modules (for ticket creation)
    [HttpGet]
    public async Task<IActionResult> GetCategoryModules(int categoryId)
    {
        try
        {
            var category = await _unitOfWork.TicketCategories.FirstOrDefaultAsync(
                x => x.Id == categoryId && x.CompanyId == _currentUserService.CompanyId && x.IsActive,
                x => x.Modules);

            if (category == null)
            {
                return Json(new { success = false, message = "Kategori bulunamadı." });
            }

            var modules = category.Modules
                .Where(m => m.IsActive)
                .OrderBy(m => m.SortOrder)
                .ThenBy(m => m.Name)
                .Select(m => new
                {
                    id = m.Id,
                    name = m.Name,
                    description = m.Description
                })
                .ToList();

            return Json(new { success = true, modules = modules });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category modules: {CategoryId}", categoryId);
            return Json(new { success = false, message = "Modüller yüklenirken bir hata oluştu." });
        }
    }
}