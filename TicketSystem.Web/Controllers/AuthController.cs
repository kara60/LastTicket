using Microsoft.AspNetCore.Mvc;
using TicketSystem.Web.Models;
using TicketSystem.Web.Services;
using System.Security.Claims;

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

        if (User.Identity?.IsAuthenticated == true)
        {
            _logger.LogInformation("User already authenticated, redirecting...");
            return RedirectToAction("Index", "Dashboard", new { area = User.IsInRole("Admin") ? "Admin" : "Customer" });
        }

        _logger.LogInformation("Showing login view");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        _logger.LogInformation("=== LOGIN POST METHOD CALLED ===");
        _logger.LogInformation("Username: {Username}", model?.Username ?? "null");
        _logger.LogInformation("ModelState.IsValid: {IsValid}", ModelState.IsValid);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("ModelState is invalid");
            foreach (var er in ModelState)
            {
                _logger.LogWarning("ModelState Error - Key: {Key}, Errors: {Errors}",
                    er.Key, string.Join(", ", er.Value.Errors.Select(e => e.ErrorMessage)));
            }
            return View(model);
        }

        _logger.LogInformation("Calling AuthService.LoginAsync...");
        var (success, token, error) = await _authService.LoginAsync(model.Username, model.Password);
        _logger.LogInformation("AuthService.LoginAsync result - Success: {Success}, Error: {Error}", success, error);

        if (success)
        {
            _logger.LogInformation("Login successful for user: {Username}", model.Username);

            Response.Cookies.Append("AuthToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(8)
            });

            _logger.LogInformation("AuthToken cookie set");

            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            var role = jwtToken.Claims.FirstOrDefault(x =>
                x.Type == ClaimTypes.Role || x.Type == "role")?.Value;

            _logger.LogInformation("User role from token: {Role}", role);

            if (role == "Admin")
            {
                _logger.LogInformation("Redirecting to Admin Dashboard");
                var redirectUrl = Url.Action("Index", "Dashboard", new { area = "Admin" });
                _logger.LogInformation("Redirect URL: {Url}", redirectUrl);
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }
            else
            {
                _logger.LogInformation("Redirecting to Customer Dashboard");
                var redirectUrl = Url.Action("Index", "Dashboard", new { area = "Customer" });
                _logger.LogInformation("Redirect URL: {Url}", redirectUrl);
                return RedirectToAction("Index", "Dashboard", new { area = "Customer" });
            }
        }

        _logger.LogWarning("Login failed for user: {Username}, Error: {Error}", model.Username, error);
        ModelState.AddModelError("", error ?? "Giriş yapılamadı.");
        return View(model);
    }

    [HttpPost]
    public IActionResult Logout()
    {
        _logger.LogInformation("=== LOGOUT METHOD CALLED ===");
        Response.Cookies.Delete("AuthToken");
        return RedirectToAction("Login");
    }

    // Test method
    [HttpGet]
    public IActionResult Test()
    {
        _logger.LogInformation("=== TEST METHOD CALLED ===");
        return Content($"AuthController çalışıyor! Time: {DateTime.Now}");
    }
}