using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.ValueObjects;

namespace TicketSystem.Infrastructure.Data.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies", "dbo");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.City).HasMaxLength(100);
        builder.Property(x => x.Country).HasMaxLength(100);
        builder.Property(x => x.PostalCode).HasMaxLength(20);
        builder.Property(x => x.Website).HasMaxLength(500);

        builder.Property(x => x.Email)
            .HasConversion(v => v.Value, v => new Email(v))
            .HasMaxLength(200);

        builder.Property(x => x.Phone)
            .HasConversion(v => v != null ? v.Value : null, v => v != null ? new PhoneNumber(v) : null);

        builder.Property(x => x.PMOApiEndpoint).HasMaxLength(1000);
        builder.Property(x => x.PMOApiKey).HasMaxLength(500);

        builder.HasIndex(x => x.Name);
        builder.HasIndex(x => x.IsActive);
    }
}