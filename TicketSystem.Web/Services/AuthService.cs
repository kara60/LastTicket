using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TicketSystem.Domain.Entities;
using TicketSystem.Application.Common.Interfaces;

namespace TicketSystem.Web.Services;

public interface IAuthService
{
    Task<(bool Success, string Token, string? Error)> LoginAsync(string username, string password);
    Task<bool> ValidateTokenAsync(string token);
    string GenerateJwtToken(User user);
}

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IUnitOfWork unitOfWork, IConfiguration configuration, ILogger<AuthService> logger)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<(bool Success, string Token, string? Error)> LoginAsync(string username, string password)
    {
        _logger.LogInformation("Login attempt for username: {Username}", username);

        try
        {
            // Kullanıcıyı bul
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(
                x => x.Username == username && x.IsActive,
                x => x.Company,
                x => x.Customer!);

            if (user == null)
            {
                _logger.LogWarning("User not found or inactive: {Username}", username);
                return (false, "", "Kullanıcı adı veya şifre hatalı.");
            }

            // Şifre kontrolü
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                _logger.LogWarning("Invalid password for user: {Username}", username);
                return (false, "", "Kullanıcı adı veya şifre hatalı.");
            }

            // Şirket kontrolü
            if (!user.Company.IsActive)
            {
                _logger.LogWarning("Company inactive for user: {Username}", username);
                return (false, "", "Şirket hesabı aktif değil.");
            }

            // Token oluştur
            var token = GenerateJwtToken(user);

            // Last login güncelle
            user.LastLoginAt = DateTime.UtcNow;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Login successful for user: {Username}, Role: {Role}", username, user.Role);
            return (true, token, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for username: {Username}", username);
            return (false, "", "Giriş işlemi sırasında bir hata oluştu.");
        }
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["JwtSettings:SecretKey"]!);

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["JwtSettings:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["JwtSettings:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Token validation failed: {Error}", ex.Message);
            return false;
        }
    }

    public string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["JwtSettings:SecretKey"]!);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email.Value),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, user.LastName),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("CompanyId", user.CompanyId.ToString()),
            new("FullName", $"{user.FirstName} {user.LastName}")
        };

        if (user.CustomerId.HasValue)
        {
            claims.Add(new Claim("CustomerId", user.CustomerId.Value.ToString()));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(double.Parse(_configuration["JwtSettings:ExpiryMinutes"]!)),
            Issuer = _configuration["JwtSettings:Issuer"],
            Audience = _configuration["JwtSettings:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        _logger.LogInformation("JWT token generated for user: {Username}", user.Username);
        return tokenString;
    }
}