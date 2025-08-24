using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.ValueObjects;
using TicketSystem.Web.Areas.Admin.Models;
using CustomerEntity = TicketSystem.Domain.Entities.Customer;

namespace TicketSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class CustomersController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<CustomersController> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var customers = await _unitOfWork.Customers.FindAsync(
                x => x.CompanyId == _currentUserService.CompanyId,
                x => x.Users);

            return View(customers.OrderBy(x => x.Name));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading customers");
            TempData["Error"] = "Müşteriler yüklenirken bir hata oluştu.";
            return View(new List<CustomerEntity>());
        }
    }

    public IActionResult Create()
    {
        return View(new CreateCustomerViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateCustomerViewModel model)
    {
        _logger.LogInformation("Creating customer: {Name}", model?.Name);

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

            // ✅ DÜZELTME: Email karşılaştırmasını client-side yap
            var normalizedEmail = model.ContactEmail.Trim().ToLower();
            var allCustomers = await _unitOfWork.Customers.FindAsync(
                x => x.CompanyId == _currentUserService.CompanyId);

            var existingCustomer = allCustomers.FirstOrDefault(
                x => x.ContactEmail.Value.ToLower() == normalizedEmail);

            if (existingCustomer != null)
            {
                ModelState.AddModelError("ContactEmail", "Bu e-posta adresiyle kayıtlı bir müşteri zaten var.");
                return View(model);
            }

            var customer = new CustomerEntity
            {
                CompanyId = _currentUserService.CompanyId.Value,
                Name = model.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
                ContactEmail = new Email(model.ContactEmail.Trim()),
                ContactPhone = string.IsNullOrWhiteSpace(model.ContactPhone) ? null : new PhoneNumber(model.ContactPhone.Trim()),
                ContactPerson = string.IsNullOrWhiteSpace(model.ContactPerson) ? null : model.ContactPerson.Trim(),
                Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim(),
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUserService.UserName ?? "System"
            };

            await _unitOfWork.Customers.AddAsync(customer);
            await _unitOfWork.SaveChangesAsync();

            TempData["Success"] = "Müşteri başarıyla oluşturuldu.";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer");
            ModelState.AddModelError("", "Müşteri oluşturulurken bir hata oluştu: " + ex.Message);
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var customer = await _unitOfWork.Customers.FirstOrDefaultAsync(
                x => x.Id == id && x.CompanyId == _currentUserService.CompanyId);

            if (customer == null)
            {
                TempData["Error"] = "Müşteri bulunamadı.";
                return RedirectToAction("Index");
            }

            var model = new EditCustomerViewModel
            {
                Id = customer.Id,
                Name = customer.Name,
                Description = customer.Description,
                ContactEmail = customer.ContactEmail.Value,
                ContactPhone = customer.ContactPhone?.Value,
                ContactPerson = customer.ContactPerson,
                Address = customer.Address,
                IsActive = customer.IsActive
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading customer for edit: {Id}", id);
            TempData["Error"] = "Müşteri yüklenirken bir hata oluştu.";
            return RedirectToAction("Index");
        }
    }

    // Edit Method için de aynı düzeltmeyi yap
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditCustomerViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var customer = await _unitOfWork.Customers.FirstOrDefaultAsync(
                x => x.Id == model.Id && x.CompanyId == _currentUserService.CompanyId);

            if (customer == null)
            {
                TempData["Error"] = "Müşteri bulunamadı.";
                return RedirectToAction("Index");
            }

            // ✅ DÜZELTME: Email karşılaştırmasını client-side yap
            var normalizedEmail = model.ContactEmail.Trim().ToLower();
            var allCustomers = await _unitOfWork.Customers.FindAsync(
                x => x.CompanyId == _currentUserService.CompanyId && x.Id != model.Id);

            var existingCustomer = allCustomers.FirstOrDefault(
                x => x.ContactEmail.Value.ToLower() == normalizedEmail);

            if (existingCustomer != null)
            {
                ModelState.AddModelError("ContactEmail", "Bu e-posta adresiyle kayıtlı başka bir müşteri var.");
                return View(model);
            }

            // Update customer
            customer.Name = model.Name.Trim();
            customer.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
            customer.ContactEmail = new Email(model.ContactEmail.Trim());
            customer.ContactPhone = string.IsNullOrWhiteSpace(model.ContactPhone) ? null : new PhoneNumber(model.ContactPhone.Trim());
            customer.ContactPerson = string.IsNullOrWhiteSpace(model.ContactPerson) ? null : model.ContactPerson.Trim();
            customer.Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim();
            customer.IsActive = model.IsActive;
            customer.UpdatedAt = DateTime.UtcNow;
            customer.UpdatedBy = _currentUserService.UserName ?? "System";

            _unitOfWork.Customers.Update(customer);
            await _unitOfWork.SaveChangesAsync();

            TempData["Success"] = "Müşteri bilgileri başarıyla güncellendi.";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer");
            ModelState.AddModelError("", "Müşteri güncellenirken bir hata oluştu: " + ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        try
        {
            var customer = await _unitOfWork.Customers.FirstOrDefaultAsync(
                x => x.Id == id && x.CompanyId == _currentUserService.CompanyId);

            if (customer == null)
            {
                TempData["Error"] = "Müşteri bulunamadı.";
                return RedirectToAction("Index");
            }

            customer.IsActive = !customer.IsActive;
            customer.UpdatedAt = DateTime.UtcNow;
            customer.UpdatedBy = _currentUserService.UserName ?? "System";

            _unitOfWork.Customers.Update(customer);
            await _unitOfWork.SaveChangesAsync();

            TempData["Success"] = $"Müşteri {(customer.IsActive ? "aktif" : "pasif")} hale getirildi.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling customer status");
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
            var customer = await _unitOfWork.Customers.FirstOrDefaultAsync(
                x => x.Id == id && x.CompanyId == _currentUserService.CompanyId);

            if (customer == null)
            {
                TempData["Error"] = "Müşteri bulunamadı.";
                return RedirectToAction("Index");
            }

            // Müşteriye bağlı kullanıcılar var mı kontrol et
            var hasUsers = await _unitOfWork.Users.ExistsAsync(u => u.CustomerId == id);
            if (hasUsers)
            {
                TempData["Error"] = "Bu müşteriye bağlı kullanıcılar var, silinemez. Önce pasif hale getirin.";
                return RedirectToAction("Index");
            }

            // Müşteriye bağlı ticketlar var mı kontrol et
            var hasTickets = await _unitOfWork.Tickets.ExistsAsync(t => t.CustomerId == id);
            if (hasTickets)
            {
                TempData["Error"] = "Bu müşteriye ait ticketlar var, silinemez. Önce pasif hale getirin.";
                return RedirectToAction("Index");
            }

            _unitOfWork.Customers.Remove(customer);
            await _unitOfWork.SaveChangesAsync();

            TempData["Success"] = "Müşteri başarıyla silindi.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting customer");
            TempData["Error"] = "Müşteri silinirken bir hata oluştu: " + ex.Message;
        }

        return RedirectToAction("Index");
    }
}