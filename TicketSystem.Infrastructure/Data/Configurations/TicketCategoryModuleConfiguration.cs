using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketSystem.Domain.Entities;

namespace TicketSystem.Infrastructure.Data.Configurations;

public class TicketCategoryModuleConfiguration : IEntityTypeConfiguration<TicketCategoryModule>
{
    public void Configure(EntityTypeBuilder<TicketCategoryModule> builder)
    {
        builder.ToTable("TicketCategoryModules", "dbo");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        builder.Property(x => x.DisplayOrder)
            .HasDefaultValue(0);

        // TicketCategory relationship
        builder.HasOne(x => x.TicketCategory)
            .WithMany(x => x.Modules)
            .HasForeignKey(x => x.TicketCategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Auditing
        builder.Property(x => x.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Indexes
        builder.HasIndex(x => x.TicketCategoryId);
        builder.HasIndex(x => new { x.TicketCategoryId, x.Name }).IsUnique();
        builder.HasIndex(x => x.DisplayOrder);
    }
}