using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.ValueObjects;

namespace TicketSystem.Infrastructure.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers", "dbo");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.ContactPerson).HasMaxLength(100);

        builder.Property(x => x.ContactEmail)
            .HasConversion(v => v.Value, v => new Email(v))
            .HasMaxLength(200);

        builder.Property(x => x.ContactPhone)
            .HasConversion(v => v != null ? v.Value : null, v => v != null ? new PhoneNumber(v) : null);

        builder.HasOne(x => x.Company)
            .WithMany(x => x.Customers)
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => new { x.CompanyId, x.Name }).IsUnique();
        builder.HasIndex(x => x.IsActive);
    }
}