using Microsoft.EntityFrameworkCore;
using TicketSystem.Application;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;
using TicketSystem.Infrastructure;
using TicketSystem.Infrastructure.Data;
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

// Request debugging middleware
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("=== REQUEST DEBUG ===");
    logger.LogInformation("Method: {Method}", context.Request.Method);
    logger.LogInformation("Path: {Path}", context.Request.Path);
    logger.LogInformation("QueryString: {QueryString}", context.Request.QueryString);
    
    if (context.Request.Method == "POST")
    {
        logger.LogInformation("Content-Type: {ContentType}", context.Request.ContentType);
    }
    
    await next();
    
    logger.LogInformation("Response Status: {StatusCode}", context.Response.StatusCode);
    logger.LogInformation("=== END REQUEST DEBUG ===");
});

// JWT Middleware
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

using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        // Database oluþtur
        await context.Database.EnsureCreatedAsync();

        // Company kontrolü
        if (!await context.Companies.AnyAsync())
        {
            logger.LogInformation("Creating demo company...");
            var company = new Company
            {
                Name = "Demo Þirketi",
                Email = "info@demo.com",
                IsActive = true,
                RequiresPMOIntegration = false,
                AutoApproveTickets = false,
                SendEmailNotifications = true,
                AllowFileAttachments = true,
                MaxFileSize = 10
            };

            context.Companies.Add(company);
            await context.SaveChangesAsync();
            logger.LogInformation("Demo company created with ID: {CompanyId}", company.Id);
        }

        // Admin user kontrolü
        if (!await context.Users.AnyAsync(u => u.Username == "admin"))
        {
            logger.LogInformation("Creating admin user...");
            var company = await context.Companies.FirstAsync();

            var adminUser = new User
            {
                Username = "admin",
                Email = "admin@demo.com",
                FirstName = "Admin",
                LastName = "User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = UserRole.Admin,
                IsActive = true,
                CompanyId = company.Id
            };

            context.Users.Add(adminUser);
            await context.SaveChangesAsync();
            logger.LogInformation("Admin user created with ID: {UserId}", adminUser.Id);
        }
        else
        {
            logger.LogInformation("Admin user already exists");
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred while seeding the database");
    }
}

app.Run();