using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;
using System.Text.Json;

namespace TicketSystem.Infrastructure.Data.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets", "dbo");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.TicketNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .HasMaxLength(2000);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(TicketStatus.İnceleniyor);

        // JSON column for dynamic form data
        builder.Property(x => x.FormData)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, JsonSerializerOptions.Default) ?? new Dictionary<string, object>()
            );

        builder.Property(x => x.SelectedModule)
            .HasMaxLength(100);

        // Company relationship
        builder.HasOne(x => x.Company)
            .WithMany(x => x.Tickets)
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Customer relationship
        builder.HasOne(x => x.Customer)
            .WithMany(x => x.Tickets)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // CreatedBy relationship
        builder.HasOne(x => x.CreatedBy)
            .WithMany(x => x.CreatedTickets)
            .HasForeignKey(x => x.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        // AssignedTo relationship
        builder.HasOne(x => x.AssignedTo)
            .WithMany(x => x.AssignedTickets)
            .HasForeignKey(x => x.AssignedToId)
            .OnDelete(DeleteBehavior.SetNull);

        // TicketType relationship
        builder.HasOne(x => x.Type)
            .WithMany(x => x.Tickets)
            .HasForeignKey(x => x.TypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Category relationship
        builder.HasOne(x => x.Category)
            .WithMany(x => x.Tickets)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Comments relationship
        builder.HasMany(x => x.Comments)
            .WithOne(x => x.Ticket)
            .HasForeignKey(x => x.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        // Attachments relationship
        builder.HasMany(x => x.Attachments)
            .WithOne(x => x.Ticket)
            .HasForeignKey(x => x.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        // History relationship
        builder.HasMany(x => x.History)
            .WithOne(x => x.Ticket)
            .HasForeignKey(x => x.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        // Auditing
        builder.Property(x => x.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Indexes
        builder.HasIndex(x => x.TicketNumber).IsUnique();
        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.CreatedById);
        builder.HasIndex(x => x.AssignedToId);
        builder.HasIndex(x => x.TypeId);
        builder.HasIndex(x => x.CategoryId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAt);
    }
}