using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;
using TicketSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace TicketSystem.Infrastructure.Data.Seeders;

public static class DataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        if (!await context.Companies.AnyAsync())
            await SeedCompanyAsync(context);

        if (!await context.Customers.AnyAsync())
            await SeedCustomersAsync(context);

        if (!await context.Users.AnyAsync())
            await SeedUsersAsync(context);

        if (!await context.TicketTypes.AnyAsync())
            await SeedTicketTypesAsync(context);

        if (!await context.TicketCategories.AnyAsync())
            await SeedTicketCategoriesAsync(context);

        await context.SaveChangesAsync();
    }

    private static async Task SeedCompanyAsync(ApplicationDbContext context)
    {
        var company = new Company
        {
            Name = "Demo Yazılım A.Ş.",
            Email = "info@demoyazilim.com",
            Phone = "+90 212 555 0101",
            Address = "Maslak Mahallesi, Teknoloji Caddesi No:1, Sarıyer/İstanbul",
            Website = "https://www.demoyazilim.com",
            IsActive = true,
            RequiresPMOIntegration = true,
            PMOApiEndpoint = "https://pmo.demoyazilim.com/api/tickets",
            PMOApiKey = "demo-api-key-12345"
        };

        await context.Companies.AddAsync(company);
    }

    private static async Task SeedCustomersAsync(ApplicationDbContext context)
    {
        var company = await context.Companies.FirstAsync();

        var customers = new[]
        {
            new Customer
            {
                CompanyId = company.Id,
                Name = "ABC Teknoloji Ltd.",
                ContactPerson = "Ahmet Yılmaz",
                ContactEmail = "ahmet@abcteknoloji.com",
                ContactPhone = "+90 216 555 0201",
                Address = "Kadıköy, İstanbul",
                IsActive = true
            },
            new Customer
            {
                CompanyId = company.Id,
                Name = "XYZ Danışmanlık A.Ş.",
                ContactPerson = "Mehmet Kaya",
                ContactEmail = "mehmet@xyzdanismanlik.com",
                ContactPhone = "+90 312 555 0301",
                Address = "Çankaya, Ankara",
                IsActive = true
            }
        };

        await context.Customers.AddRangeAsync(customers);
    }

    private static async Task SeedUsersAsync(ApplicationDbContext context)
    {
        var company = await context.Companies.FirstAsync();
        var customer = await context.Customers.FirstAsync();

        var users = new List<User>
        {
            new User
            {
                CompanyId = company.Id,
                FirstName = "Admin",
                LastName = "User",
                Username = "admin",
                Email = "admin@demoyazilim.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = UserRole.Admin,
                IsActive = true
            },
            new User
            {
                CompanyId = company.Id,
                CustomerId = customer.Id,
                FirstName = "Ahmet",
                LastName = "Yılmaz",
                Username = "ahmet",
                Email = "ahmet@abcteknoloji.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer123!"),
                Role = UserRole.Customer,
                IsActive = true
            }
        };

        await context.Users.AddRangeAsync(users);
    }

    private static async Task SeedTicketTypesAsync(ApplicationDbContext context)
    {
        var company = await context.Companies.FirstAsync();

        var ticketTypes = new[]
        {
            new TicketType
            {
                CompanyId = company.Id,
                Name = "Hata Bildirimi",
                Description = "Sistemde karşılaşılan hatalar için",
                Icon = "bug",
                Color = "#ef4444",
                IsActive = true,
                SortOrder = 1,
                FormDefinition = JsonSerializer.Serialize(new
                {
                    fields = new[]
                    {
                        new { name = "hataBaslik", label = "Hata Başlığı", type = "text", required = true },
                        new { name = "hataAciklama", label = "Hata Açıklaması", type = "textarea", required = true }
                    }
                })
            },
            new TicketType
            {
                CompanyId = company.Id,
                Name = "Yeni Özellik",
                Description = "Yeni özellik talepleri için",
                Icon = "lightbulb",
                Color = "#3b82f6",
                IsActive = true,
                SortOrder = 2,
                FormDefinition = JsonSerializer.Serialize(new
                {
                    fields = new[]
                    {
                        new { name = "ozellikBaslik", label = "Özellik Başlığı", type = "text", required = true },
                        new { name = "ozellikAciklama", label = "Özellik Açıklaması", type = "textarea", required = true }
                    }
                })
            }
        };

        await context.TicketTypes.AddRangeAsync(ticketTypes);
    }

    private static async Task SeedTicketCategoriesAsync(ApplicationDbContext context)
    {
        var company = await context.Companies.FirstAsync();
        var type = await context.TicketTypes.OrderBy(x => x.SortOrder).FirstAsync();

        var categories = new[]
        {
            new TicketCategory
            {
                CompanyId = company.Id,
                TicketTypeId = type.Id,
                Name = "Web Uygulaması",
                Description = "Web tabanlı uygulamalar",
                Icon = "globe",
                Color = "#6366f1",
                IsActive = true,
                SortOrder = 1
            },
            new TicketCategory
            {
                CompanyId = company.Id,
                TicketTypeId = type.Id,
                Name = "Mobil Uygulama",
                Description = "iOS ve Android uygulamalar",
                Icon = "device-phone-mobile",
                Color = "#8b5cf6",
                IsActive = true,
                SortOrder = 2
            }
        };

        await context.TicketCategories.AddRangeAsync(categories);
        await context.SaveChangesAsync();

        var webCategory = await context.TicketCategories.FirstAsync();

        var modules = new[]
        {
            new TicketCategoryModule { TicketCategoryId = webCategory.Id, Name = "Kullanıcı Yönetimi", IsActive = true, SortOrder = 1 },
            new TicketCategoryModule { TicketCategoryId = webCategory.Id, Name = "Raporlama", IsActive = true, SortOrder = 2 }
        };

        await context.TicketCategoryModules.AddRangeAsync(modules);
    }
}