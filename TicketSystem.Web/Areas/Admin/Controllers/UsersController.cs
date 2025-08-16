using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;
using TicketSystem.Web.Areas.Admin.Models;

namespace TicketSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class UsersController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<UsersController> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _unitOfWork.Users.GetAllAsync(x => x.Customer!);
        return View(users);
    }

    public async Task<IActionResult> Create()
    {
        var customers = await _unitOfWork.Customers.FindAsync(x => x.IsActive);
        ViewBag.Customers = customers;
        return View(new CreateUserViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        _logger.LogInformation("=== CREATE USER POST ===");
        _logger.LogInformation("Model values - FirstName: {FirstName}, LastName: {LastName}, Email: {Email}, Username: {Username}, Role: {Role}, CustomerId: {CustomerId}",
            model?.FirstName, model?.LastName, model?.Email, model?.Username, model?.Role, model?.CustomerId);

        // Model state validation check
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

            var customers = await _unitOfWork.Customers.FindAsync(x => x.IsActive);
            ViewBag.Customers = customers;
            return View(model);
        }

        // Additional validations
        if (model.Role == UserRole.Customer && !model.CustomerId.HasValue)
        {
            ModelState.AddModelError("CustomerId", "Müşteri rolü için müşteri seçimi gereklidir.");
            var customers = await _unitOfWork.Customers.FindAsync(x => x.IsActive);
            ViewBag.Customers = customers;
            return View(model);
        }

        try
        {
            // CompanyId kontrolü
            if (!_currentUserService.CompanyId.HasValue)
            {
                _logger.LogError("CompanyId is null for current user");
                ModelState.AddModelError("", "Şirket bilgisi alınamadı. Lütfen tekrar giriş yapın.");
                var customers = await _unitOfWork.Customers.FindAsync(x => x.IsActive);
                ViewBag.Customers = customers;
                return View(model);
            }

            // Username kontrolü - bu çalışıyor
            var existingUser = await _unitOfWork.Users.FirstOrDefaultAsync(x => x.Username == model.Username.Trim().ToLowerInvariant());
            if (existingUser != null)
            {
                ModelState.AddModelError("Username", "Bu kullanıcı adı zaten kullanılmaktadır.");
                var customers = await _unitOfWork.Customers.FindAsync(x => x.IsActive);
                ViewBag.Customers = customers;
                return View(model);
            }

            // Email kontrolü - DÜZELTME: Tüm kullanıcıları memory'e alıp sonra kontrol ediyoruz
            var allUsers = await _unitOfWork.Users.GetAllAsync();
            var normalizedEmail = model.Email.Trim().ToLowerInvariant();
            var existingEmailUser = allUsers.FirstOrDefault(x => x.Email.Value.ToLowerInvariant() == normalizedEmail);

            if (existingEmailUser != null)
            {
                ModelState.AddModelError("Email", "Bu e-posta adresi zaten kullanılmaktadır.");
                var customers = await _unitOfWork.Customers.FindAsync(x => x.IsActive);
                ViewBag.Customers = customers;
                return View(model);
            }

            _logger.LogInformation("Creating user with CompanyId: {CompanyId}", _currentUserService.CompanyId);

            // User oluşturma
            var user = new User
            {
                CompanyId = _currentUserService.CompanyId.Value,
                CustomerId = model.Role == UserRole.Customer ? model.CustomerId : null,
                FirstName = model.FirstName.Trim(),
                LastName = model.LastName.Trim(),
                Email = normalizedEmail, // Value Object otomatik olarak oluşturulacak
                Username = model.Username.Trim().ToLowerInvariant(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Role = model.Role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUserService.UserId?.ToString()
            };

            _logger.LogInformation("User object created successfully");

            await _unitOfWork.Users.AddAsync(user);
            var saveResult = await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("User saved to database. SaveChanges result: {SaveResult}", saveResult);

            TempData["Success"] = "Kullanıcı başarıyla oluşturuldu.";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            ModelState.AddModelError("", "Kullanıcı oluşturulurken bir hata oluştu: " + ex.Message);
            var customers = await _unitOfWork.Customers.FindAsync(x => x.IsActive);
            ViewBag.Customers = customers;
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, x => x.Customer!);
        if (user == null)
        {
            TempData["Error"] = "Kullanıcı bulunamadı.";
            return RedirectToAction("Index");
        }

        var customers = await _unitOfWork.Customers.FindAsync(x => x.IsActive);
        ViewBag.Customers = customers;

        var model = new EditUserViewModel
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email.Value,
            Username = user.Username,
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
            var customers = await _unitOfWork.Customers.FindAsync(x => x.IsActive);
            ViewBag.Customers = customers;
            return View(model);
        }

        // Additional validations
        if (model.Role == UserRole.Customer && !model.CustomerId.HasValue)
        {
            ModelState.AddModelError("CustomerId", "Müşteri rolü için müşteri seçimi gereklidir.");
            var customers = await _unitOfWork.Customers.FindAsync(x => x.IsActive);
            ViewBag.Customers = customers;
            return View(model);
        }

        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(model.Id);
            if (user == null)
            {
                TempData["Error"] = "Kullanıcı bulunamadı.";
                return RedirectToAction("Index");
            }

            // Username değişmişse kontrolü
            var normalizedUsername = model.Username.Trim().ToLowerInvariant();
            if (user.Username != normalizedUsername)
            {
                var existingUser = await _unitOfWork.Users.FirstOrDefaultAsync(x => x.Username == normalizedUsername && x.Id != model.Id);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Username", "Bu kullanıcı adı zaten kullanılmaktadır.");
                    var customers = await _unitOfWork.Customers.FindAsync(x => x.IsActive);
                    ViewBag.Customers = customers;
                    return View(model);
                }
            }

            // Email değişmişse kontrolü - DÜZELTME
            var normalizedEmail = model.Email.Trim().ToLowerInvariant();
            if (user.Email.Value.ToLowerInvariant() != normalizedEmail)
            {
                var allUsers = await _unitOfWork.Users.GetAllAsync();
                var existingEmailUser = allUsers.FirstOrDefault(x =>
                    x.Email.Value.ToLowerInvariant() == normalizedEmail && x.Id != model.Id);

                if (existingEmailUser != null)
                {
                    ModelState.AddModelError("Email", "Bu e-posta adresi zaten kullanılmaktadır.");
                    var customers = await _unitOfWork.Customers.FindAsync(x => x.IsActive);
                    ViewBag.Customers = customers;
                    return View(model);
                }
            }

            user.FirstName = model.FirstName.Trim();
            user.LastName = model.LastName.Trim();
            user.Email = normalizedEmail;
            user.Username = normalizedUsername;
            user.Role = model.Role;
            user.CustomerId = model.Role == UserRole.Customer ? model.CustomerId : null;
            user.IsActive = model.IsActive;
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = _currentUserService.UserId?.ToString();

            // Şifre değişmişse güncelle
            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            }

            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            TempData["Success"] = "Kullanıcı başarıyla güncellendi.";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user");
            ModelState.AddModelError("", "Kullanıcı güncellenirken bir hata oluştu: " + ex.Message);
            var customers = await _unitOfWork.Customers.FindAsync(x => x.IsActive);
            ViewBag.Customers = customers;
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "Kullanıcı bulunamadı.";
                return RedirectToAction("Index");
            }

            user.IsActive = !user.IsActive;
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = _currentUserService.UserId?.ToString();

            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            TempData["Success"] = $"Kullanıcı {(user.IsActive ? "aktif" : "pasif")} edildi.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling user status");
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
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "Kullanıcı bulunamadı.";
                return RedirectToAction("Index");
            }

            // Kendi hesabını silemesin
            if (user.Id == _currentUserService.UserId)
            {
                TempData["Error"] = "Kendi hesabınızı silemezsiniz.";
                return RedirectToAction("Index");
            }

            _unitOfWork.Users.Remove(user);
            await _unitOfWork.SaveChangesAsync();

            TempData["Success"] = "Kullanıcı başarıyla silindi.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user");
            TempData["Error"] = "Kullanıcı silinirken bir hata oluştu: " + ex.Message;
        }

        return RedirectToAction("Index");
    }
}