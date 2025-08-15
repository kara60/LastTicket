using TicketSystem.Domain.Common;
using TicketSystem.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Domain.Entities;

public class Ticket : AuditableEntity
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public TicketStatus Status { get; set; } = TicketStatus.İnceleniyor;

    [MaxLength(20)]
    public string TicketNumber { get; set; } = string.Empty;

    // Form Data (JSON)
    public string? FormData { get; set; }

    // Selected Module
    public string? SelectedModule { get; set; }

    // Dates
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime? DueDate { get; set; }

    // Resolution & Comments
    [MaxLength(2000)]
    public string? Resolution { get; set; }
    public DateTime? ResolvedAt { get; set; }

    // Internal Notes (Only visible to admins)
    public string? InternalNotes { get; set; }

    // Customer feedback
    public int? CustomerRating { get; set; } // 1-5 stars
    public string? CustomerFeedback { get; set; }

    // Foreign Keys
    public int CompanyId { get; set; }
    public int? CustomerId { get; set; }
    public int TypeId { get; set; }
    public int CategoryId { get; set; }
    public int? TicketCategoryModuleId { get; set; }
    public int CreatedByUserId { get; set; }
    public int? AssignedToUserId { get; set; }

    // Navigation Properties
    public virtual Company Company { get; set; } = null!;
    public virtual Customer? Customer { get; set; }
    public virtual TicketType Type { get; set; } = null!;
    public virtual TicketCategory Category { get; set; } = null!;
    public virtual TicketCategoryModule? TicketCategoryModule { get; set; }
    public virtual User CreatedBy { get; set; } = null!;
    public virtual User? AssignedTo { get; set; }

    public virtual ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
    public virtual ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();
    public virtual ICollection<TicketHistory> History { get; set; } = new List<TicketHistory>();

    // Business Methods
    public void Create()
    {
        Status = TicketStatus.İnceleniyor;
        SubmittedAt = DateTime.UtcNow;

        if (string.IsNullOrEmpty(TicketNumber))
        {
            TicketNumber = GenerateTicketNumber();
        }
    }

    public void Approve(int approvedByUserId, string? approvalComment = null)
    {
        if (Status != TicketStatus.İnceleniyor)
            throw new InvalidOperationException("Sadece 'İnceleniyor' durumundaki ticketlar onaylanabilir");

        Status = TicketStatus.İşlemde;
        ApprovedAt = DateTime.UtcNow;
        AssignedToUserId = approvedByUserId;
        UpdatedBy = approvedByUserId.ToString();

        if (!string.IsNullOrEmpty(approvalComment))
        {
            AddComment($"Ticket onaylandı: {approvalComment}", approvedByUserId, isInternal: true);
        }
    }

    public void Reject(int rejectedByUserId, string rejectionReason)
    {
        if (Status != TicketStatus.İnceleniyor && Status != TicketStatus.İşlemde)
            throw new InvalidOperationException("Sadece 'İnceleniyor' veya 'İşlemde' durumundaki ticketlar reddedilebilir");

        if (string.IsNullOrWhiteSpace(rejectionReason))
            throw new ArgumentException("Red sebesi zorunludur", nameof(rejectionReason));

        Status = TicketStatus.Reddedildi;
        RejectedAt = DateTime.UtcNow;
        Resolution = rejectionReason;
        ResolvedAt = DateTime.UtcNow;
        UpdatedBy = rejectedByUserId.ToString();

        AddComment($"Ticket reddedildi: {rejectionReason}", rejectedByUserId, isInternal: false);
    }

    public void Resolve(int resolvedByUserId, string? resolutionComment = null)
    {
        if (Status != TicketStatus.İşlemde)
            throw new InvalidOperationException("Sadece 'İşlemde' durumundaki ticketlar çözülebilir");

        Status = TicketStatus.Çözüldü;
        CompletedAt = DateTime.UtcNow;
        ResolvedAt = DateTime.UtcNow;
        UpdatedBy = resolvedByUserId.ToString();

        if (!string.IsNullOrEmpty(resolutionComment))
        {
            Resolution = resolutionComment;
            AddComment($"Ticket çözüldü: {resolutionComment}", resolvedByUserId, isInternal: false);
        }
    }

    public void Close(int closedByUserId, string? closingComment = null)
    {
        if (Status != TicketStatus.Çözüldü)
            throw new InvalidOperationException("Sadece 'Çözüldü' durumundaki ticketlar kapatılabilir");

        Status = TicketStatus.Kapandı;
        ClosedAt = DateTime.UtcNow;
        UpdatedBy = closedByUserId.ToString();

        if (!string.IsNullOrEmpty(closingComment))
        {
            AddComment($"Ticket kapatıldı: {closingComment}", closedByUserId, isInternal: false);
        }
    }

    public void AddComment(string content, int userId, bool isInternal = false)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Yorum içeriği boş olamaz", nameof(content));

        var comment = new TicketComment
        {
            Content = content,
            TicketId = Id,
            UserId = userId,
            IsInternal = isInternal,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId.ToString()
        };

        Comments.Add(comment);
    }

    private string GenerateTicketNumber()
    {
        var year = DateTime.UtcNow.Year;
        var timestamp = DateTime.UtcNow.ToString("MMddHHmmss");
        return $"TK{year}{timestamp}";
    }

    // Computed Properties
    public bool IsOpen => Status != TicketStatus.Kapandı && Status != TicketStatus.Reddedildi;

    public bool CanTakeAction => Status == TicketStatus.İnceleniyor ||
                                Status == TicketStatus.İşlemde ||
                                Status == TicketStatus.Çözüldü;

    public List<string> AvailableActions
    {
        get
        {
            return Status switch
            {
                TicketStatus.İnceleniyor => new List<string> { "Onay", "Reddet" },
                TicketStatus.İşlemde => new List<string> { "Çözüldü", "Reddet" },
                TicketStatus.Çözüldü => new List<string> { "Ticket Kapat" },
                _ => new List<string>()
            };
        }
    }

    public TimeSpan? TimeToResolve => ResolvedAt.HasValue && SubmittedAt.HasValue
        ? ResolvedAt - SubmittedAt
        : null;

    public bool IsOverdue => DueDate.HasValue && DueDate < DateTime.UtcNow && IsOpen;

    public string StatusDisplayText => Status switch
    {
        TicketStatus.İnceleniyor => "İnceleniyor",
        TicketStatus.İşlemde => "İşlemde",
        TicketStatus.Çözüldü => "Çözüldü",
        TicketStatus.Kapandı => "Kapandı",
        TicketStatus.Reddedildi => "Reddedildi",
        _ => "Bilinmiyor"
    };

    public string StatusColor => Status switch
    {
        TicketStatus.İnceleniyor => "#f59e0b", // Orange
        TicketStatus.İşlemde => "#3b82f6",     // Blue
        TicketStatus.Çözüldü => "#10b981",     // Green
        TicketStatus.Kapandı => "#6b7280",     // Gray
        TicketStatus.Reddedildi => "#ef4444",  // Red
        _ => "#6b7280"
    };
}