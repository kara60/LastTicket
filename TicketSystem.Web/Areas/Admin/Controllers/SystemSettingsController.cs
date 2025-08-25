using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Web.Areas.Admin.Models;
using TicketSystem.Domain.ValueObjects;

namespace TicketSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class SystemSettingsController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<SystemSettingsController> _logger;

    public SystemSettingsController(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<SystemSettingsController> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var company = await _unitOfWork.Companies.GetByIdAsync(_currentUserService.CompanyId!.Value);

            if (company == null)
            {
                _logger.LogError("Company not found for current user. CompanyId: {CompanyId}", _currentUserService.CompanyId);
                TempData["Error"] = "Şirket bilgisi bulunamadı.";
                return RedirectToAction("Index", "Dashboard");
            }

            var model = new SystemSettingsViewModel
            {
                CompanyId = company.Id,
                CompanyName = company.Name,
                Description = company.Description,
                Email = company.Email?.Value ?? string.Empty,
                Phone = company.Phone?.Value ?? string.Empty,
                Address = company.Address,
                City = company.City,
                Country = company.Country,
                PostalCode = company.PostalCode,
                Website = company.Website,

                // System Settings
                RequiresPMOIntegration = company.RequiresPMOIntegration,
                AutoApproveTickets = company.AutoApproveTickets,
                SendEmailNotifications = company.SendEmailNotifications,
                AllowFileAttachments = company.AllowFileAttachments,
                MaxFileSize = company.MaxFileSize,
                PMOApiEndpoint = company.PMOApiEndpoint,
                PMOApiKey = company.PMOApiKey
            };

            _logger.LogInformation("System settings loaded for company: {CompanyName} (ID: {CompanyId})",
                company.Name, company.Id);

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading system settings");
            TempData["Error"] = "Sistem ayarları yüklenirken bir hata oluştu.";
            return RedirectToAction("Index", "Dashboard");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(SystemSettingsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("System settings form validation failed. Errors: {Errors}",
                string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return View("Index", model);
        }

        try
        {
            var company = await _unitOfWork.Companies.GetByIdAsync(_currentUserService.CompanyId!.Value);

            if (company == null)
            {
                _logger.LogError("Company not found for update. CompanyId: {CompanyId}", _currentUserService.CompanyId);
                ModelState.AddModelError("", "Şirket bilgisi bulunamadı.");
                return View("Index", model);
            }

            // Update company info
            company.Name = model.CompanyName.Trim();
            company.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
            company.Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim();
            company.City = string.IsNullOrWhiteSpace(model.City) ? null : model.City.Trim();
            company.Country = string.IsNullOrWhiteSpace(model.Country) ? null : model.Country.Trim();
            company.PostalCode = string.IsNullOrWhiteSpace(model.PostalCode) ? null : model.PostalCode.Trim();
            company.Website = string.IsNullOrWhiteSpace(model.Website) ? null : model.Website.Trim();

            // Update email and phone with value objects
            try
            {
                company.Email = new Email(model.Email.Trim());
                company.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : new PhoneNumber(model.Phone.Trim());
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", "Geçersiz email veya telefon formatı: " + ex.Message);
                return View("Index", model);
            }

            // Update system settings
            company.RequiresPMOIntegration = model.RequiresPMOIntegration;
            company.AutoApproveTickets = model.AutoApproveTickets;
            company.SendEmailNotifications = model.SendEmailNotifications;
            company.AllowFileAttachments = model.AllowFileAttachments;
            company.MaxFileSize = model.MaxFileSize;

            // PMO Integration settings
            if (model.RequiresPMOIntegration)
            {
                if (string.IsNullOrWhiteSpace(model.PMOApiEndpoint))
                {
                    ModelState.AddModelError("PMOApiEndpoint", "PMO entegrasyonu aktifken API endpoint gereklidir.");
                    return View("Index", model);
                }

                company.PMOApiEndpoint = model.PMOApiEndpoint.Trim();
                company.PMOApiKey = string.IsNullOrWhiteSpace(model.PMOApiKey) ? null : model.PMOApiKey.Trim();
            }
            else
            {
                company.PMOApiEndpoint = null;
                company.PMOApiKey = null;
            }

            // Update audit fields
            company.UpdatedAt = DateTime.UtcNow;
            company.UpdatedBy = _currentUserService.UserName ?? "System";

            _unitOfWork.Companies.Update(company);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("System settings updated successfully for company: {CompanyName} (ID: {CompanyId}) by user: {UserName}",
                company.Name, company.Id, _currentUserService.UserName);

            TempData["Success"] = "Sistem ayarları başarıyla güncellendi.";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating system settings for company: {CompanyId}", _currentUserService.CompanyId);
            ModelState.AddModelError("", "Sistem ayarları güncellenirken bir hata oluştu: " + ex.Message);
            return View("Index", model);
        }
    }

    [HttpPost]
    public async Task<IActionResult> TestPMOConnection()
    {
        try
        {
            var company = await _unitOfWork.Companies.GetByIdAsync(_currentUserService.CompanyId!.Value);

            if (company == null || string.IsNullOrEmpty(company.PMOApiEndpoint))
            {
                return Json(new { success = false, message = "PMO API endpoint tanımlanmamış." });
            }

            // Basic connectivity test - You can implement actual API test here
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var response = await httpClient.GetAsync(company.PMOApiEndpoint);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("PMO connection test successful for company: {CompanyId}", company.Id);
                return Json(new { success = true, message = "PMO bağlantısı başarılı!" });
            }
            else
            {
                _logger.LogWarning("PMO connection test failed. Status: {StatusCode} for company: {CompanyId}",
                    response.StatusCode, company.Id);
                return Json(new { success = false, message = $"PMO bağlantısı başarısız. Status: {response.StatusCode}" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing PMO connection for company: {CompanyId}", _currentUserService.CompanyId);
            return Json(new { success = false, message = "Bağlantı testi sırasında hata oluştu: " + ex.Message });
        }
    }
}