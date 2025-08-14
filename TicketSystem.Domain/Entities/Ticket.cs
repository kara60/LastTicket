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
    public TicketStatus Status { get; set; } = TicketStatus.İnceleniyor; // Başlangıç durumu

    [MaxLength(20)]
    public string TicketNumber { get; set; } = string.Empty;

    // Form Data (JSON)
    public string? FormData { get; set; }

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
    public int CustomerId { get; set; }
    public int TicketTypeId { get; set; }
    public int TicketCategoryId { get; set; }
    public int? TicketCategoryModuleId { get; set; }
    public int CreatedByUserId { get; set; }
    public int? AssignedToUserId { get; set; }

    // Navigation Properties
    public virtual Company Company { get; set; } = null!;
    public virtual Customer Customer { get; set; } = null!;
    public virtual TicketType TicketType { get; set; } = null!;
    public virtual TicketCategory TicketCategory { get; set; } = null!;
    public virtual TicketCategoryModule? TicketCategoryModule { get; set; }
    public virtual User CreatedByUser { get; set; } = null!;
    public virtual User? AssignedToUser { get; set; }

    public virtual ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
    public virtual ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();
    public virtual ICollection<TicketHistory> History { get; set; } = new List<TicketHistory>();

    // ================================
    // BUSINESS METHODS - YENİ FLOW
    // ================================

    /// <summary>
    /// Ticket oluşturulduğunda otomatik çağrılır - İnceleniyor status'ü ile başlar
    /// </summary>
    public void Create()
    {
        Status = TicketStatus.İnceleniyor;
        SubmittedAt = DateTime.UtcNow;

        // Generate ticket number
        if (string.IsNullOrEmpty(TicketNumber))
        {
            TicketNumber = GenerateTicketNumber();
        }
    }

    /// <summary>
    /// Admin İnceleniyor status'ünden ONAYLAR → İşlemde
    /// </summary>
    public void Approve(int approvedByUserId, string? approvalComment = null)
    {
        if (Status != TicketStatus.İnceleniyor)
            throw new InvalidOperationException("Sadece 'İnceleniyor' durumundaki ticketlar onaylanabilir");

        Status = TicketStatus.İşlemde;
        ApprovedAt = DateTime.UtcNow;
        AssignedToUserId = approvedByUserId; // Admin kendine atayabilir veya başkasına
        UpdatedBy = approvedByUserId.ToString();

        // Onay yorumu varsa ekle
        if (!string.IsNullOrEmpty(approvalComment))
        {
            AddComment($"Ticket onaylandı: {approvalComment}", approvedByUserId, isInternal: true);
        }
    }

    /// <summary>
    /// Admin İnceleniyor veya İşlemde status'ünden REDDEDİR
    /// </summary>
    public void Reject(int rejectedByUserId, string rejectionReason)
    {
        if (Status != TicketStatus.İnceleniyor && Status != TicketStatus.İşlemde)
            throw new InvalidOperationException("Sadece 'İnceleniyor' veya 'İşlemde' durumundaki ticketlar reddedilebilir");

        if (string.IsNullOrWhiteSpace(rejectionReason))
            throw new ArgumentException("Red sebesi zorunludur", nameof(rejectionReason));

        Status = TicketStatus.Reddedildi; // Enum'a eklenecek
        RejectedAt = DateTime.UtcNow;
        Resolution = rejectionReason;
        ResolvedAt = DateTime.UtcNow;
        UpdatedBy = rejectedByUserId.ToString();

        // Red yorumu ekle
        AddComment($"Ticket reddedildi: {rejectionReason}", rejectedByUserId, isInternal: false);
    }

    /// <summary>
    /// Admin İşlemde status'ünden ÇÖZER → Çözüldü
    /// </summary>
    public void Resolve(int resolvedByUserId, string? resolutionComment = null)
    {
        if (Status != TicketStatus.İşlemde)
            throw new InvalidOperationException("Sadece 'İşlemde' durumundaki ticketlar çözülebilir");

        Status = TicketStatus.Çözüldü;
        CompletedAt = DateTime.UtcNow;
        ResolvedAt = DateTime.UtcNow;
        UpdatedBy = resolvedByUserId.ToString();

        // Çözüm yorumu varsa ekle
        if (!string.IsNullOrEmpty(resolutionComment))
        {
            Resolution = resolutionComment;
            AddComment($"Ticket çözüldü: {resolutionComment}", resolvedByUserId, isInternal: false);
        }
    }

    /// <summary>
    /// Admin Çözüldü status'ünden KAPATIR → Kapandı
    /// </summary>
    public void Close(int closedByUserId, string? closingComment = null)
    {
        if (Status != TicketStatus.Çözüldü)
            throw new InvalidOperationException("Sadece 'Çözüldü' durumundaki ticketlar kapatılabilir");

        Status = TicketStatus.Kapandı;
        ClosedAt = DateTime.UtcNow;
        UpdatedBy = closedByUserId.ToString();

        // Kapatma yorumu varsa ekle
        if (!string.IsNullOrEmpty(closingComment))
        {
            AddComment($"Ticket kapatıldı: {closingComment}", closedByUserId, isInternal: false);
        }
    }

    /// <summary>
    /// Yorum ekleme - hem internal hem public
    /// </summary>
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

    /// <summary>
    /// Ticket numarası üretici
    /// </summary>
    private string GenerateTicketNumber()
    {
        var year = DateTime.UtcNow.Year;
        var timestamp = DateTime.UtcNow.ToString("MMddHHmmss");
        return $"TK{year}{timestamp}";
    }

    // ================================
    // COMPUTED PROPERTIES
    // ================================

    /// <summary>
    /// Ticket açık mı? (Kapanmamış veya reddedilmemiş)
    /// </summary>
    public bool IsOpen => Status != TicketStatus.Kapandı && Status != TicketStatus.Reddedildi;

    /// <summary>
    /// Admin aksiyon alabilir mi?
    /// </summary>
    public bool CanTakeAction => Status == TicketStatus.İnceleniyor ||
                                Status == TicketStatus.İşlemde ||
                                Status == TicketStatus.Çözüldü;

    /// <summary>
    /// Hangi butonlar gösterilecek?
    /// </summary>
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

    /// <summary>
    /// Ticket süresi hesaplama
    /// </summary>
    public TimeSpan? TimeToResolve => ResolvedAt.HasValue && SubmittedAt.HasValue
        ? ResolvedAt - SubmittedAt
        : null;

    /// <summary>
    /// Ticket gecikmede mi?
    /// </summary>
    public bool IsOverdue => DueDate.HasValue && DueDate < DateTime.UtcNow && IsOpen;

    /// <summary>
    /// Status display metni
    /// </summary>
    public string StatusDisplayText => Status switch
    {
        TicketStatus.İnceleniyor => "İnceleniyor",
        TicketStatus.İşlemde => "İşlemde",
        TicketStatus.Çözüldü => "Çözüldü",
        TicketStatus.Kapandı => "Kapandı",
        TicketStatus.Reddedildi => "Reddedildi", // Enum'a eklenecek
        _ => "Bilinmiyor"
    };

    /// <summary>
    /// Status renk kodu (UI için)
    /// </summary>
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