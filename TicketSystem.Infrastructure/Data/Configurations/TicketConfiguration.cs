using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;

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

        // JSON column for dynamic form data (stored as text/jsonb)
        builder.Property(x => x.FormData)
            .HasColumnType("jsonb");

        builder.Property(x => x.SelectedModule)
            .HasMaxLength(100);

        // Company
        builder.HasOne(x => x.Company)
            .WithMany(x => x.Tickets)
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Customer
        builder.HasOne(x => x.Customer)
            .WithMany(x => x.Tickets)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // CreatedBy
        builder.HasOne(x => x.CreatedBy)
            .WithMany(x => x.CreatedTickets)
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // AssignedTo
        builder.HasOne(x => x.AssignedTo)
            .WithMany(x => x.AssignedTickets)
            .HasForeignKey(x => x.AssignedToUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // TicketType
        builder.HasOne(x => x.Type)
            .WithMany(x => x.Tickets)
            .HasForeignKey(x => x.TypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Category
        builder.HasOne(x => x.Category)
            .WithMany(x => x.Tickets)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Category Module
        builder.HasOne(x => x.TicketCategoryModule)
            .WithMany(x => x.Tickets)
            .HasForeignKey(x => x.TicketCategoryModuleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Comments
        builder.HasMany(x => x.Comments)
            .WithOne(x => x.Ticket)
            .HasForeignKey(x => x.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        // Attachments
        builder.HasMany(x => x.Attachments)
            .WithOne(x => x.Ticket)
            .HasForeignKey(x => x.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        // History
        builder.HasMany(x => x.History)
            .WithOne(x => x.Ticket)
            .HasForeignKey(x => x.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.TicketNumber).IsUnique();
        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.CreatedByUserId);
        builder.HasIndex(x => x.AssignedToUserId);
        builder.HasIndex(x => x.TypeId);
        builder.HasIndex(x => x.CategoryId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAt);
    }
}