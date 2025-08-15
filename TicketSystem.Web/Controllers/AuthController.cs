using Microsoft.AspNetCore.Mvc;
using TicketSystem.Web.Models;
using TicketSystem.Web.Services;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

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
    public IActionResult Login()
    {
        _logger.LogInformation("=== LOGIN GET METHOD CALLED ===");

        // Zaten giriş yapmış kullanıcıyı yönlendir
        if (User.Identity?.IsAuthenticated == true)
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            _logger.LogInformation("User already authenticated with role: {Role}", userRole);

            if (userRole == "Admin")
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }
            else
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Customer" });
            }
        }

        _logger.LogInformation("Showing login view");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        _logger.LogInformation("=== LOGIN POST METHOD CALLED ===");
        _logger.LogInformation("Username: {Username}", model?.Username ?? "null");
        _logger.LogInformation("ModelState.IsValid: {IsValid}", ModelState.IsValid);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("ModelState is invalid");
            foreach (var error in ModelState)
            {
                _logger.LogWarning("ModelState Error - Key: {Key}, Errors: {Errors}",
                    error.Key, string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage)));
            }
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

                // Cookie ayarları
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = Request.IsHttps,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddHours(8)
                };

                Response.Cookies.Append("AuthToken", token, cookieOptions);

                // Token'dan role bilgisini al
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);
                var role = jwtToken.Claims.FirstOrDefault(x =>
                    x.Type == ClaimTypes.Role || x.Type == "role")?.Value;

                _logger.LogInformation("User role from token: {Role}", role);

                // Role'e göre yönlendir
                if (role == "Admin")
                {
                    _logger.LogInformation("Redirecting to Admin Dashboard");
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                }
                else if (role == "Customer")
                {
                    _logger.LogInformation("Redirecting to Customer Dashboard");
                    return RedirectToAction("Index", "Dashboard", new { area = "Customer" });
                }
                else
                {
                    _logger.LogWarning("Unknown role: {Role}", role);
                    ModelState.AddModelError("", "Geçersiz kullanıcı rolü.");
                    return View(model);
                }
            }
            else
            {
                _logger.LogWarning("Login failed for user: {Username}, Error: {Error}", model.Username, error);
                ModelState.AddModelError("", error ?? "Giriş yapılamadı.");
                return View(model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during login for user: {Username}", model.Username);
            ModelState.AddModelError("", "Giriş işlemi sırasında bir hata oluştu.");
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        _logger.LogInformation("=== LOGOUT METHOD CALLED ===");

        // Cookie'yi sil
        Response.Cookies.Delete("AuthToken");

        _logger.LogInformation("User logged out, redirecting to login");
        return RedirectToAction("Login");
    }

    // Test endpoint
    [HttpGet]
    public IActionResult Test()
    {
        _logger.LogInformation("=== TEST METHOD CALLED ===");
        return Json(new
        {
            message = "AuthController çalışıyor!",
            time = DateTime.Now,
            isAuthenticated = User.Identity?.IsAuthenticated ?? false,
            userName = User.Identity?.Name,
            role = User.FindFirstValue(ClaimTypes.Role)
        });
    }
}