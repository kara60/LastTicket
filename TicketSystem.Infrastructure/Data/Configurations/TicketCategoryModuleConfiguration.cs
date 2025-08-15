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
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        builder.Property(x => x.SortOrder)
            .HasDefaultValue(0);

        builder.HasOne(x => x.TicketCategory)
            .WithMany(x => x.Modules)
            .HasForeignKey(x => x.TicketCategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.TicketCategoryId);
        builder.HasIndex(x => new { x.TicketCategoryId, x.Name }).IsUnique();
        builder.HasIndex(x => x.SortOrder);
    }
}