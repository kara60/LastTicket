using Microsoft.EntityFrameworkCore;
using TicketSystem.Domain.Common;
using TicketSystem.Domain.Entities;
using TicketSystem.Infrastructure.Data.Configurations;

namespace TicketSystem.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<User> Users => Set<User>();
    public DbSet<TicketType> TicketTypes => Set<TicketType>();
    public DbSet<TicketCategory> TicketCategories => Set<TicketCategory>();
    public DbSet<TicketCategoryModule> TicketCategoryModules => Set<TicketCategoryModule>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketComment> TicketComments => Set<TicketComment>();
    public DbSet<TicketAttachment> TicketAttachments => Set<TicketAttachment>();
    public DbSet<TicketHistory> TicketHistory => Set<TicketHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations
        modelBuilder.ApplyConfiguration(new CompanyConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new TicketTypeConfiguration());
        modelBuilder.ApplyConfiguration(new TicketCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new TicketCategoryModuleConfiguration());
        modelBuilder.ApplyConfiguration(new TicketConfiguration());
        modelBuilder.ApplyConfiguration(new TicketCommentConfiguration());
        modelBuilder.ApplyConfiguration(new TicketAttachmentConfiguration());
        modelBuilder.ApplyConfiguration(new TicketHistoryConfiguration());

        // Global settings
        ConfigureGlobalSettings(modelBuilder);
    }

    private void ConfigureGlobalSettings(ModelBuilder modelBuilder)
    {
        // PostgreSQL specific settings
        modelBuilder.HasDefaultSchema("dbo");

        // Configure all DateTime properties to use timestamp with time zone
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetColumnType("timestamp with time zone");
                }
            }
        }

        // Configure all string properties default collation for Turkish characters
        foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(t => t.GetProperties()).Where(p => p.ClrType == typeof(string)))
        {
            property.SetCollation("tr-TR-x-icu");
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await UpdateAuditableEntities();
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        UpdateAuditableEntities().GetAwaiter().GetResult();
        return base.SaveChanges();
    }

    private async Task UpdateAuditableEntities()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is BaseEntity entity)
            {
                if (entry.State == EntityState.Added)
                {
                    entity.CreatedAt = DateTime.UtcNow;
                }
                entity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}