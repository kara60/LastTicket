using Microsoft.AspNetCore.Mvc;
using TicketSystem.Web.Models;
using TicketSystem.Web.Services;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;

namespace TicketSystem.Web.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        _logger.LogInformation("=== LOGIN GET METHOD CALLED ===");
        _logger.LogInformation("Return URL: {ReturnUrl}", returnUrl);

        // Zaten giriş yapmış kullanıcıyı yönlendir
        if (User.Identity?.IsAuthenticated == true)
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            _logger.LogInformation("User already authenticated with role: {Role}", userRole);

            // Return URL varsa oraya yönlendir, yoksa role'e göre dashboard'a git
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToRoleBasedDashboard(userRole);
        }

        ViewData["ReturnUrl"] = returnUrl;
        _logger.LogInformation("Showing login view");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        _logger.LogInformation("=== LOGIN POST METHOD CALLED ===");
        _logger.LogInformation("Username: {Username}", model?.Username ?? "null");
        _logger.LogInformation("Return URL: {ReturnUrl}", returnUrl);
        _logger.LogInformation("ModelState.IsValid: {IsValid}", ModelState.IsValid);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("ModelState is invalid");
            foreach (var error in ModelState)
            {
                _logger.LogWarning("ModelState Error - Key: {Key}, Errors: {Errors}",
                    error.Key, string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage)));
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        try
        {
            _logger.LogInformation("Calling AuthService.LoginAsync for user: {Username}", model.Username);
            var (success, token, error) = await _authService.LoginAsync(model.Username, model.Password);

            _logger.LogInformation("AuthService.LoginAsync result - Success: {Success}", success);

            if (success && !string.IsNullOrEmpty(token))
            {
                _logger.LogInformation("Login successful, setting auth cookie");

                // Cookie ayarları - güvenlik açısından iyileştirildi
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = Request.IsHttps, // HTTPS'de true olmalı
                    SameSite = SameSiteMode.Strict,
                    Expires = model.RememberMe
                        ? DateTimeOffset.UtcNow.AddDays(30)
                        : DateTimeOffset.UtcNow.AddHours(8),
                    Path = "/"
                };

                Response.Cookies.Append("AuthToken", token, cookieOptions);

                // Token'dan role bilgisini al
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);
                var role = jwtToken.Claims.FirstOrDefault(x =>
                    x.Type == ClaimTypes.Role || x.Type == "role")?.Value;

                _logger.LogInformation("User role from token: {Role}", role);

                // TempData ile başarı mesajı
                TempData["Success"] = "Başarıyla giriş yaptınız!";

                // Return URL varsa oraya yönlendir, yoksa role'e göre dashboard'a git
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    _logger.LogInformation("Redirecting to return URL: {ReturnUrl}", returnUrl);
                    return Redirect(returnUrl);
                }

                return RedirectToRoleBasedDashboard(role);
            }
            else
            {
                _logger.LogWarning("Login failed for user: {Username}, Error: {Error}", model.Username, error);
                ModelState.AddModelError("", error ?? "Giriş yapılamadı.");
                ViewData["ReturnUrl"] = returnUrl;
                return View(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during login for user: {Username}", model.Username);
            ModelState.AddModelError("", "Giriş işlemi sırasında bir hata oluştu. Lütfen tekrar deneyiniz.");
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        try
        {
            // Tüm cookie'leri temizle
            var cookiesToDelete = new[]
            {
            "AuthToken",
            "RefreshToken",
            ".AspNetCore.Session",     // Session cookie
            ".AspNetCore.Cookies",
            ".AspNetCore.Antiforgery"
        };

            var cookieOptions = new CookieOptions
            {
                Path = "/",
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                HttpOnly = true,
                Expires = DateTime.Now.AddDays(-1) // Geçmişte tarih = hemen sil
            };

            foreach (var cookieName in cookiesToDelete)
            {
                if (Request.Cookies.ContainsKey(cookieName))
                {
                    Response.Cookies.Delete(cookieName, cookieOptions);
                }
            }

            // Session'ı tamamen temizle
            HttpContext.Session.Clear();

            // Browser cache temizle
            Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
            Response.Headers.Add("Pragma", "no-cache");
            Response.Headers.Add("Expires", "0");

            TempData["Success"] = "Başarıyla çıkış yaptınız.";
            return RedirectToAction("Login");
        }
        catch
        {
            // Hata olsa bile login'e yönlendir
            return RedirectToAction("Login");
        }
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        _logger.LogWarning("Access denied for user: {User}", User.Identity?.Name ?? "Anonymous");
        TempData["Error"] = "Bu sayfaya erişim yetkiniz bulunmamaktadır.";

        if (User.Identity?.IsAuthenticated == true)
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            return RedirectToRoleBasedDashboard(role);
        }

        return RedirectToAction("Login");
    }

    // Test endpoint - sadece development'ta
    [HttpGet]
    public IActionResult Test()
    {
        if (!HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
        {
            return NotFound();
        }

        _logger.LogInformation("=== TEST METHOD CALLED ===");

        var cookieValue = Request.Cookies["AuthToken"];

        return Json(new
        {
            message = "AuthController çalışıyor!",
            time = DateTime.Now,
            isAuthenticated = User.Identity?.IsAuthenticated ?? false,
            userName = User.Identity?.Name,
            role = User.FindFirstValue(ClaimTypes.Role),
            userId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            companyId = User.FindFirstValue("CompanyId"),
            customerId = User.FindFirstValue("CustomerId"),
            hasCookie = !string.IsNullOrEmpty(cookieValue),
            cookieLength = cookieValue?.Length ?? 0,
            claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
        });
    }

    #region Private Methods

    private IActionResult RedirectToRoleBasedDashboard(string? role)
    {
        return role switch
        {
            "Admin" => RedirectToAction("Index", "Dashboard", new { area = "Admin" }),
            "Customer" => RedirectToAction("Index", "Dashboard", new { area = "Customer" }),
            _ => throw new InvalidOperationException($"Unknown role: {role}")
        };
    }

    #endregion
}