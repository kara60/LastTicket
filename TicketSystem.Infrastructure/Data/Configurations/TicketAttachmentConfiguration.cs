using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketSystem.Domain.Entities;

namespace TicketSystem.Infrastructure.Data.Configurations;

public class TicketAttachmentConfiguration : IEntityTypeConfiguration<TicketAttachment>
{
    public void Configure(EntityTypeBuilder<TicketAttachment> builder)
    {
        builder.ToTable("TicketAttachments", "dbo");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.FilePath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.FileSize)
            .IsRequired();

        // Ticket relationship
        builder.HasOne(x => x.Ticket)
            .WithMany(x => x.Attachments)
            .HasForeignKey(x => x.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        // UploadedBy relationship
        builder.HasOne(x => x.UploadedBy)
            .WithMany(x => x.Attachments)
            .HasForeignKey(x => x.UploadedById)
            .OnDelete(DeleteBehavior.Restrict);

        // Auditing
        builder.Property(x => x.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Indexes
        builder.HasIndex(x => x.TicketId);
        builder.HasIndex(x => x.UploadedById);
        builder.HasIndex(x => x.CreatedAt);
    }
}