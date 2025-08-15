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

        // Seed Company
        if (!await context.Companies.AnyAsync())
        {
            await SeedCompanyAsync(context);
        }

        // Seed Customers
        if (!await context.Customers.AnyAsync())
        {
            await SeedCustomersAsync(context);
        }

        // Seed Users
        if (!await context.Users.AnyAsync())
        {
            await SeedUsersAsync(context);
        }

        // Seed Ticket Types
        if (!await context.TicketTypes.AnyAsync())
        {
            await SeedTicketTypesAsync(context);
        }

        // Seed Ticket Categories
        if (!await context.TicketCategories.AnyAsync())
        {
            await SeedTicketCategoriesAsync(context);
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedCompanyAsync(ApplicationDbContext context)
    {
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Demo Yazılım A.Ş.",
            Email = "info@demoyazilim.com",
            Phone = "+90 212 555 0101",
            Address = "Maslak Mahallesi, Teknoloji Caddesi No:1, Sarıyer/İstanbul",
            WebSite = "https://www.demoyazilim.com",
            IsActive = true,
            PmoIntegrationEnabled = true,
            PmoApiEndpoint = "https://pmo.demoyazilim.com/api/tickets",
            PmoApiKey = "demo-api-key-12345"
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
                Id = Guid.NewGuid(),
                CompanyId = company.Id,
                Name = "ABC Teknoloji Ltd.",
                ContactPersonName = "Ahmet Yılmaz",
                Email = "ahmet@abcteknoloji.com",
                Phone = "+90 216 555 0201",
                Address = "Kadıköy, İstanbul",
                IsActive = true
            },
            new Customer
            {
                Id = Guid.NewGuid(),
                CompanyId = company.Id,
                Name = "XYZ Danışmanlık A.Ş.",
                ContactPersonName = "Mehmet Kaya",
                Email = "mehmet@xyzdanismanlik.com",
                Phone = "+90 312 555 0301",
                Address = "Çankaya, Ankara",
                IsActive = true
            },
            new Customer
            {
                Id = Guid.NewGuid(),
                CompanyId = company.Id,
                Name = "DEF Holding",
                ContactPersonName = "Ayşe Demir",
                Email = "ayse@defholding.com",
                Phone = "+90 232 555 0401",
                Address = "Konak, İzmir",
                IsActive = true
            }
        };

        await context.Customers.AddRangeAsync(customers);
    }

    private static async Task SeedUsersAsync(ApplicationDbContext context)
    {
        var company = await context.Companies.FirstAsync();
        var customers = await context.Customers.ToListAsync();

        var users = new List<User>();

        // Admin kullanıcı
        users.Add(new User
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            FirstName = "Admin",
            LastName = "User",
            Email = "admin@demoyazilim.com",
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"), // Gerçek projede güçlü hash kullanın
            Role = UserRole.Admin,
            IsActive = true
        });

        // Customer kullanıcıları
        foreach (var customer in customers)
        {
            // Her customer için 2 kullanıcı
            users.Add(new User
            {
                Id = Guid.NewGuid(),
                CompanyId = company.Id,
                CustomerId = customer.Id,
                FirstName = customer.ContactPersonName.Split(' ')[0],
                LastName = customer.ContactPersonName.Split(' ')[1],
                Email = customer.Email,
                Username = customer.Email.Split('@')[0],
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer123!"),
                Role = UserRole.Customer,
                IsActive = true
            });

            // İkinci kullanıcı
            users.Add(new User
            {
                Id = Guid.NewGuid(),
                CompanyId = company.Id,
                CustomerId = customer.Id,
                FirstName = "Test",
                LastName = "User",
                Email = $"test@{customer.Email.Split('@')[1]}",
                Username = $"test{customer.Email.Split('@')[0]}",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer123!"),
                Role = UserRole.Customer,
                IsActive = true
            });
        }

        await context.Users.AddRangeAsync(users);
    }

    private static async Task SeedTicketTypesAsync(ApplicationDbContext context)
    {
        var company = await context.Companies.FirstAsync();

        var ticketTypes = new[]
        {
            new TicketType
            {
                Id = Guid.NewGuid(),
                CompanyId = company.Id,
                Name = "Hata Bildirimi",
                Description = "Sistemde karşılaşılan hatalar için",
                Icon = "bug",
                Color = "#ef4444",
                IsActive = true,
                DisplayOrder = 1,
                FormDefinition = JsonSerializer.Serialize(new
                {
                    fields = new[]
                    {
                        new { name = "hataBaslik", label = "Hata Başlığı", type = "text", required = true },
                        new { name = "hataAciklama", label = "Hata Açıklaması", type = "textarea", required = true },
                        new { name = "hataAdimlar", label = "Hatayı Alma Adımları", type = "textarea", required = true },
                        new { name = "tarayici", label = "Tarayıcı", type = "select", options = new[] { "Chrome", "Firefox", "Safari", "Edge" } },
                        new { name = "isletimSistemi", label = "İşletim Sistemi", type = "select", options = new[] { "Windows", "macOS", "Linux" } }
                    }
                })
            },
            new TicketType
            {
                Id = Guid.NewGuid(),
                CompanyId = company.Id,
                Name = "Yeni Özellik",
                Description = "Yeni özellik talepleri için",
                Icon = "lightbulb",
                Color = "#3b82f6",
                IsActive = true,
                DisplayOrder = 2,
                FormDefinition = JsonSerializer.Serialize(new
                {
                    fields = new[]
                    {
                        new { name = "ozellikBaslik", label = "Özellik Başlığı", type = "text", required = true },
                        new { name = "ozellikAciklama", label = "Özellik Açıklaması", type = "textarea", required = true },
                        new { name = "isTanimi", label = "İş Tanımı", type = "textarea", required = true },
                        new { name = "oncelik", label = "Öncelik", type = "select", options = new[] { "Düşük", "Orta", "Yüksek", "Kritik" } },
                        new { name = "hedefTarih", label = "Hedef Tarih", type = "date" }
                    }
                })
            },
            new TicketType
            {
                Id = Guid.NewGuid(),
                CompanyId = company.Id,
                Name = "Danışmanlık/Eğitim",
                Description = "Danışmanlık ve eğitim talepleri için",
                Icon = "academic-cap",
                Color = "#10b981",
                IsActive = true,
                DisplayOrder = 3,
                FormDefinition = JsonSerializer.Serialize(new
                {
                    fields = new[]
                    {
                        new { name = "konu", label = "Konu", type = "text", required = true },
                        new { name = "egitimTuru", label = "Eğitim Türü", type = "select", options = new[] { "Online", "Yüz Yüze", "Hibrit" }, required = true },
                        new { name = "katilimciSayisi", label = "Katılımcı Sayısı", type = "number", required = true },
                        new { name = "egitimTarihi", label = "Eğitim Tarihi", type = "date" },
                        new { name = "sure", label = "Süre (Saat)", type = "number" },
                        new { name = "notlar", label = "Ek Notlar", type = "textarea" }
                    }
                })
            },
            new TicketType
            {
                Id = Guid.NewGuid(),
                CompanyId = company.Id,
                Name = "Bakım/Güncelleme",
                Description = "Sistem bakım ve güncelleme talepleri için",
                Icon = "cog",
                Color = "#f59e0b",
                IsActive = true,
                DisplayOrder = 4,
                FormDefinition = JsonSerializer.Serialize(new
                {
                    fields = new[]
                    {
                        new { name = "bakimTuru", label = "Bakım Türü", type = "select", options = new[] { "Güncelleme", "Performans İyileştirme", "Güvenlik Yaması", "Veri Yedekleme" }, required = true },
                        new { name = "aciklama", label = "Açıklama", type = "textarea", required = true },
                        new { name = "planlananTarih", label = "Planlanan Tarih", type = "date" },
                        new { name = "tahminiSure", label = "Tahmini Süre", type = "text" },
                        new { name = "etkilenecekSistemler", label = "Etkilenecek Sistemler", type = "textarea" }
                    }
                })
            }
        };

        await context.TicketTypes.AddRangeAsync(ticketTypes);
    }

    private static async Task SeedTicketCategoriesAsync(ApplicationDbContext context)
    {
        var company = await context.Companies.FirstAsync();

        var categories = new[]
        {
            new TicketCategory
            {
                Id = Guid.NewGuid(),
                CompanyId = company.Id,
                Name = "Web Uygulaması",
                Description = "Web tabanlı uygulamalar",
                Icon = "globe",
                Color = "#6366f1",
                IsActive = true,
                DisplayOrder = 1
            },
            new TicketCategory
            {
                Id = Guid.NewGuid(),
                CompanyId = company.Id,
                Name = "Mobil Uygulama",
                Description = "iOS ve Android uygulamalar",
                Icon = "device-phone-mobile",
                Color = "#8b5cf6",
                IsActive = true,
                DisplayOrder = 2
            },
            new TicketCategory
            {
                Id = Guid.NewGuid(),
                CompanyId = company.Id,
                Name = "API Servisleri",
                Description = "Backend API servisleri",
                Icon = "code-bracket",
                Color = "#06b6d4",
                IsActive = true,
                DisplayOrder = 3
            },
            new TicketCategory
            {
                Id = Guid.NewGuid(),
                CompanyId = company.Id,
                Name = "Veritabanı",
                Description = "Veritabanı işlemleri",
                Icon = "circle-stack",
                Color = "#84cc16",
                IsActive = true,
                DisplayOrder = 4
            }
        };

        await context.TicketCategories.AddRangeAsync(categories);
        await context.SaveChangesAsync();

        // Kategorilere modüller ekle
        var webCategory = categories[0];
        var mobileCategory = categories[1];
        var apiCategory = categories[2];
        var dbCategory = categories[3];

        var modules = new[]
        {
            // Web Uygulaması modülleri
            new TicketCategoryModule { Id = Guid.NewGuid(), TicketCategoryId = webCategory.Id, Name = "Kullanıcı Yönetimi", IsActive = true, DisplayOrder = 1 },
            new TicketCategoryModule { Id = Guid.NewGuid(), TicketCategoryId = webCategory.Id, Name = "Raporlama", IsActive = true, DisplayOrder = 2 },
            new TicketCategoryModule { Id = Guid.NewGuid(), TicketCategoryId = webCategory.Id, Name = "Dashboard", IsActive = true, DisplayOrder = 3 },
            new TicketCategoryModule { Id = Guid.NewGuid(), TicketCategoryId = webCategory.Id, Name = "Entegrasyonlar", IsActive = true, DisplayOrder = 4 },

            // Mobil Uygulama modülleri
            new TicketCategoryModule { Id = Guid.NewGuid(), TicketCategoryId = mobileCategory.Id, Name = "iOS App", IsActive = true, DisplayOrder = 1 },
            new TicketCategoryModule { Id = Guid.NewGuid(), TicketCategoryId = mobileCategory.Id, Name = "Android App", IsActive = true, DisplayOrder = 2 },
            new TicketCategoryModule { Id = Guid.NewGuid(), TicketCategoryId = mobileCategory.Id, Name = "Push Notifications", IsActive = true, DisplayOrder = 3 },

            // API Servisleri modülleri
            new TicketCategoryModule { Id = Guid.NewGuid(), TicketCategoryId = apiCategory.Id, Name = "Authentication API", IsActive = true, DisplayOrder = 1 },
            new TicketCategoryModule { Id = Guid.NewGuid(), TicketCategoryId = apiCategory.Id, Name = "Business API", IsActive = true, DisplayOrder = 2 },
            new TicketCategoryModule { Id = Guid.NewGuid(), TicketCategoryId = apiCategory.Id, Name = "Reporting API", IsActive = true, DisplayOrder = 3 },

            // Veritabanı modülleri
            new TicketCategoryModule { Id = Guid.NewGuid(), TicketCategoryId = dbCategory.Id, Name = "SQL Server", IsActive = true, DisplayOrder = 1 },
            new TicketCategoryModule { Id = Guid.NewGuid(), TicketCategoryId = dbCategory.Id, Name = "PostgreSQL", IsActive = true, DisplayOrder = 2 },
            new TicketCategoryModule { Id = Guid.NewGuid(), TicketCategoryId = dbCategory.Id, Name = "MongoDB", IsActive = true, DisplayOrder = 3 }
        };

        await context.TicketCategoryModules.AddRangeAsync(modules);
    }
}