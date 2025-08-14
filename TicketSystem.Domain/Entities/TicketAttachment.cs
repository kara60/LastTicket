using TicketSystem.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Domain.Entities;

public class TicketAttachment : AuditableEntity
{
    [Required, MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    [Required]
    public long FileSizeBytes { get; set; }

    [Required, MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [MaxLength(64)]
    public string? FileHash { get; set; } // For duplicate detection

    public bool IsImage { get; set; } = false;

    // Foreign Keys
    public int TicketId { get; set; }
    public int UploadedByUserId { get; set; }

    // Navigation Properties
    public virtual Ticket Ticket { get; set; } = null!;
    public virtual User UploadedByUser { get; set; } = null!;

    // Computed Properties
    public string FileSizeFormatted
    {
        get
        {
            var size = FileSizeBytes;
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size = size / 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }
    }
}