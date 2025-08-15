using Microsoft.EntityFrameworkCore;
using TicketSystem.Application;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;
using TicketSystem.Infrastructure;
using TicketSystem.Infrastructure.Data;
using TicketSystem.Infrastructure.Data.Seeders;
using TicketSystem.Web.Extensions;
using TicketSystem.Web.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// Application Services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddWebServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// JWT Middleware - Authentication'dan önce olmalý
app.UseMiddleware<JwtMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// Area routing
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// Default routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

// Database Migration ve Seed
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Starting database operations...");

        // Veritabanýný oluþtur ve migration'larý uygula
        await context.Database.MigrateAsync();
        logger.LogInformation("Database migration completed");

        // Seed data
        await SeedInitialDataAsync(context, logger);
        //await DataSeeder.SeedAsync(context);

        logger.LogInformation("Database operations completed successfully");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred while setting up the database");
        throw; // Uygulama baþlamasýn hata varsa
    }
}

app.Run();

// Seed Methods
static async Task SeedInitialDataAsync(ApplicationDbContext context, ILogger logger)
{
    // Company kontrolü ve oluþturma
    var company = await context.Companies.FirstOrDefaultAsync();
    if (company == null)
    {
        logger.LogInformation("Creating initial company...");
        company = new Company
        {
            Name = "Demo Yazýlým A.Þ.",
            Email = "info@demoyazilim.com",
            Phone = "+90 212 555 0101",
            Address = "Maslak Mahallesi, Teknoloji Caddesi No:1",
            City = "Ýstanbul",
            Country = "Türkiye",
            PostalCode = "34485",
            Website = "https://www.demoyazilim.com",
            IsActive = true,
            RequiresPMOIntegration = false,
            AutoApproveTickets = false,
            SendEmailNotifications = true,
            AllowFileAttachments = true,
            MaxFileSize = 10,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        };

        context.Companies.Add(company);
        await context.SaveChangesAsync();
        logger.LogInformation("Company created with ID: {CompanyId}", company.Id);
    }
    else
    {
        logger.LogInformation("Company already exists with ID: {CompanyId}", company.Id);
    }

    // Customer kontrolü ve oluþturma
    var customer = await context.Customers.FirstOrDefaultAsync(c => c.CompanyId == company.Id);
    if (customer == null)
    {
        logger.LogInformation("Creating initial customer...");
        customer = new Customer
        {
            CompanyId = company.Id,
            Name = "ABC Teknoloji Ltd.",
            ContactPerson = "Ahmet Yýlmaz",
            ContactEmail = "ahmet@abcteknoloji.com",
            ContactPhone = "+90 216 555 0201",
            Address = "Kadýköy, Ýstanbul",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        };

        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        logger.LogInformation("Customer created with ID: {CustomerId}", customer.Id);
    }
    else
    {
        logger.LogInformation("Customer already exists with ID: {CustomerId}", customer.Id);
    }

    // Admin user kontrolü ve oluþturma
    var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
    if (adminUser == null)
    {
        logger.LogInformation("Creating admin user...");
        adminUser = new User
        {
            Username = "admin",
            Email = "admin@demoyazilim.com",
            FirstName = "System",
            LastName = "Admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Role = UserRole.Admin,
            IsActive = true,
            CompanyId = company.Id,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        };

        context.Users.Add(adminUser);
        await context.SaveChangesAsync();
        logger.LogInformation("Admin user created - Username: admin, Password: Admin123!");
    }
    else
    {
        logger.LogInformation("Admin user already exists with ID: {UserId}", adminUser.Id);
    }

    // Customer user kontrolü ve oluþturma
    var customerUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "ahmet");
    if (customerUser == null)
    {
        logger.LogInformation("Creating customer user...");
        customerUser = new User
        {
            Username = "ahmet",
            Email = "ahmet@abcteknoloji.com",
            FirstName = "Ahmet",
            LastName = "Yýlmaz",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer123!"),
            Role = UserRole.Customer,
            IsActive = true,
            CompanyId = company.Id,
            CustomerId = customer.Id,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        };

        context.Users.Add(customerUser);
        await context.SaveChangesAsync();
        logger.LogInformation("Customer user created - Username: ahmet, Password: Customer123!");
    }
    else
    {
        logger.LogInformation("Customer user already exists with ID: {UserId}", customerUser.Id);
    }

    logger.LogInformation("=== LOGIN CREDENTIALS ===");
    logger.LogInformation("Admin: admin / Admin123!");
    logger.LogInformation("Customer: ahmet / Customer123!");
    logger.LogInformation("========================");
}