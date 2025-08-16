using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace TicketSystem.Web.Middleware;

public class JwtMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtMiddleware> _logger;

    public JwtMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<JwtMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var token = ExtractToken(context);

        if (!string.IsNullOrEmpty(token))
        {
            await AttachUserToContextAsync(context, token);
        }

        await _next(context);
    }

    private string? ExtractToken(HttpContext context)
    {
        // 1. Cookie'den token al (öncelik)
        var cookieToken = context.Request.Cookies["AuthToken"];
        if (!string.IsNullOrEmpty(cookieToken))
        {
            return cookieToken;
        }

        // 2. Authorization header'dan token al
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            return authHeader.Substring("Bearer ".Length);
        }

        return null;
    }

    private async Task AttachUserToContextAsync(HttpContext context, string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["JwtSettings:SecretKey"]!);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["JwtSettings:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["JwtSettings:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                RequireExpirationTime = true,
                RequireSignedTokens = true
            };

            // Token'ı doğrula
            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

            if (validatedToken is JwtSecurityToken jwtToken)
            {
                // Algoritma kontrolü
                if (!jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    _logger.LogWarning("JWT token has invalid algorithm: {Algorithm}", jwtToken.Header.Alg);
                    ClearAuthCookie(context);
                    return;
                }

                // Claims'leri al
                var claims = jwtToken.Claims.ToList();

                // ÖNEMLİ: Identity'yi doğru şekilde oluştur
                // AuthenticationType belirtmek çok kritik!
                var identity = new ClaimsIdentity(claims, "Bearer", ClaimTypes.NameIdentifier, ClaimTypes.Role);

                // Principal oluştur ve context'e ata
                context.User = new ClaimsPrincipal(identity);

                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = context.User.FindFirst(ClaimTypes.Name)?.Value;
                var role = context.User.FindFirst(ClaimTypes.Role)?.Value;

                _logger.LogDebug("JWT token validated successfully - User: {UserId}/{UserName}, Role: {Role}, IsAuthenticated: {IsAuthenticated}",
                    userId, userName, role, context.User.Identity.IsAuthenticated);
            }
        }
        catch (SecurityTokenExpiredException ex)
        {
            _logger.LogInformation("JWT token expired: {Message}", ex.Message);
            ClearAuthCookie(context);
        }
        catch (SecurityTokenValidationException ex)
        {
            _logger.LogWarning("JWT token validation failed: {Message}", ex.Message);
            ClearAuthCookie(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during JWT token validation");
            ClearAuthCookie(context);
        }
    }

    private void ClearAuthCookie(HttpContext context)
    {
        context.Response.Cookies.Delete("AuthToken", new CookieOptions
        {
            Path = "/",
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            HttpOnly = true
        });
    }
}