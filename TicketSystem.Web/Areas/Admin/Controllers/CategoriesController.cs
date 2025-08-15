using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;
using TicketSystem.Infrastructure.Repositories;
using TicketSystem.Web.Areas.Admin.Models;

namespace TicketSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class CategoriesController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public CategoriesController(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<IActionResult> Index()
    {
        var categories = await _unitOfWork.TicketCategories.FindAsync(
            x => x.CompanyId == _currentUserService.CompanyId,
            x => x.Modules);
        return View(categories.OrderBy(x => x.SortOrder));
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

        var category = new TicketCategory
        {
            Id = -1,
            CompanyId = _currentUserService.CompanyId!.Value,
            Name = model.Name,
            Description = model.Description,
            Icon = model.Icon,
            Color = model.Color,
            IsActive = true,
            SortOrder = model.DisplayOrder
        };

        await _unitOfWork.TicketCategories.AddAsync(category);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "Kategori başarıyla oluşturuldu.";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Edit(int id)
    {
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(
            x => x.Id == id && x.CompanyId == _currentUserService.CompanyId,
            x => x.Customer!);

        if (user == null)
        {
            return NotFound();
        }

        var customers = await _unitOfWork.Customers.FindAsync(
            x => x.CompanyId == _currentUserService.CompanyId && x.IsActive);
        ViewBag.Customers = customers;

        var model = new EditUserViewModel
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role,
            CustomerId = user.CustomerId,
            IsActive = user.IsActive
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var customers = await _unitOfWork.Customers.FindAsync(
                x => x.CompanyId == _currentUserService.CompanyId && x.IsActive);
            ViewBag.Customers = customers;
            return View(model);
        }

        var user = await _unitOfWork.Users.FirstOrDefaultAsync(
            x => x.Id == model.Id && x.CompanyId == _currentUserService.CompanyId);

        if (user == null)
        {
            return NotFound();
        }

        // Check if username already exists (excluding current user)
        var existingUser = await _unitOfWork.Users.FirstOrDefaultAsync(
            x => x.CompanyId == _currentUserService.CompanyId && x.Id != model.Id);
        if (existingUser != null)
        {
            ModelState.AddModelError("Username", "Bu kullanıcı adı zaten kullanılıyor.");
            var customers = await _unitOfWork.Customers.FindAsync(
                x => x.CompanyId == _currentUserService.CompanyId && x.IsActive);
            ViewBag.Customers = customers;
            return View(model);
        }

        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.Email = model.Email;
        user.Role = model.Role;
        user.CustomerId = model.Role == UserRole.Customer ? model.CustomerId : null;
        user.IsActive = model.IsActive;

        // Update password if provided
        if (!string.IsNullOrEmpty(model.NewPassword))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
        }

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "Kullanıcı başarıyla güncellendi.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(
            x => x.Id == id && x.CompanyId == _currentUserService.CompanyId);

        if (user == null)
        {
            return NotFound();
        }

        // Don't allow deleting current user
        if (user.Id == _currentUserService.UserId)
        {
            TempData["Error"] = "Kendi hesabınızı silemezsiniz.";
            return RedirectToAction("Index");
        }

        // Check if user has tickets
        var hasTickets = await _unitOfWork.Tickets.ExistsAsync(t => t.CreatedByUserId == id || t.AssignedToUserId == id);
        if (hasTickets)
        {
            TempData["Error"] = "Bu kullanıcının ticket'ları var, silinemez. Önce pasif hale getirin.";
            return RedirectToAction("Index");
        }

        _unitOfWork.Users.Remove(user);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "Kullanıcı başarıyla silindi.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(
            x => x.Id == id && x.CompanyId == _currentUserService.CompanyId);

        if (user == null)
        {
            return NotFound();
        }

        // Don't allow deactivating current user
        if (user.Id == _currentUserService.UserId)
        {
            TempData["Error"] = "Kendi hesabınızı pasif hale getiremezsiniz.";
            return RedirectToAction("Index");
        }

        user.IsActive = !user.IsActive;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = $"Kullanıcı {(user.IsActive ? "aktif" : "pasif")} hale getirildi.";
        return RedirectToAction("Index");
    }
}