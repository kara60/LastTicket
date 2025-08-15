using Microsoft.AspNetCore.Mvc;
using TicketSystem.Web.Models;
using TicketSystem.Web.Services;
using System.Security.Claims;

namespace TicketSystem.Web.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard", new { area = User.IsInRole("Admin") ? "Admin" : "Customer" });
        }

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (success, token, error) = await _authService.LoginAsync(model.Username, model.Password);

        if (success)
        {
            Response.Cookies.Append("AuthToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(1)
            });

            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            var role = jwtToken.Claims.FirstOrDefault(x =>
                x.Type == ClaimTypes.Role || x.Type == "role")?.Value;

            if (role == "Admin")
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            else
                return RedirectToAction("Index", "Dashboard", new { area = "Customer" });
        }

        ModelState.AddModelError("", error ?? "Giriş yapılamadı.");
        return View(model);
    }

    [HttpPost]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("AuthToken");
        return RedirectToAction("Login");
    }
}