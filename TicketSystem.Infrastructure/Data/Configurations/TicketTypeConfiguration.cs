using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketSystem.Domain.Entities;

namespace TicketSystem.Infrastructure.Data.Configurations;

public class TicketTypeConfiguration : IEntityTypeConfiguration<TicketType>
{
    public void Configure(EntityTypeBuilder<TicketType> builder)
    {
        builder.ToTable("TicketTypes", "dbo");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.Icon).HasMaxLength(50);
        builder.Property(x => x.Color).HasMaxLength(20);

        builder.Property(x => x.CardTitle).HasMaxLength(200);
        builder.Property(x => x.CardDescription).HasMaxLength(500);
        builder.Property(x => x.CardSubtitle).HasMaxLength(200);

        builder.Property(x => x.SortOrder).HasDefaultValue(0);
        builder.Property(x => x.IsActive).HasDefaultValue(true);

        // Keep FormDefinition as text (string). If you prefer postgres jsonb:
        // builder.Property(x => x.FormDefinition).HasColumnType("jsonb");

        builder.HasOne(x => x.Company)
            .WithMany(x => x.TicketTypes)
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => new { x.CompanyId, x.Name }).IsUnique();
    }
}