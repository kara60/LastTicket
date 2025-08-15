using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketSystem.Domain.Entities;

namespace TicketSystem.Infrastructure.Data.Configurations;

public class TicketCategoryConfiguration : IEntityTypeConfiguration<TicketCategory>
{
    public void Configure(EntityTypeBuilder<TicketCategory> builder)
    {
        builder.ToTable("TicketCategories", "dbo");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.Icon)
            .HasMaxLength(50);

        builder.Property(x => x.Color)
            .HasMaxLength(20);

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        builder.Property(x => x.DisplayOrder)
            .HasDefaultValue(0);

        // Company relationship
        builder.HasOne(x => x.Company)
            .WithMany(x => x.TicketCategories)
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // TicketCategoryModules relationship
        builder.HasMany(x => x.Modules)
            .WithOne(x => x.TicketCategory)
            .HasForeignKey(x => x.TicketCategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Tickets relationship
        builder.HasMany(x => x.Tickets)
            .WithOne(x => x.Category)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Auditing
        builder.Property(x => x.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Indexes
        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => new { x.CompanyId, x.Name }).IsUnique();
        builder.HasIndex(x => x.DisplayOrder);
    }
}