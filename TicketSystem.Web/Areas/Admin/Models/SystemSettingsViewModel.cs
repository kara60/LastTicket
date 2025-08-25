using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Web.Areas.Admin.Models;

public class SystemSettingsViewModel
{
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "Şirket adı gereklidir.")]
    [StringLength(200, ErrorMessage = "Şirket adı en fazla 200 karakter olabilir.")]
    [Display(Name = "Şirket Adı")]
    public string CompanyName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "E-posta adresi gereklidir.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    [StringLength(100, ErrorMessage = "E-posta adresi en fazla 100 karakter olabilir.")]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
    [StringLength(20, ErrorMessage = "Telefon numarası en fazla 20 karakter olabilir.")]
    [Display(Name = "Telefon")]
    public string? Phone { get; set; }

    [StringLength(500, ErrorMessage = "Adres en fazla 500 karakter olabilir.")]
    [Display(Name = "Adres")]
    public string? Address { get; set; }

    [StringLength(100, ErrorMessage = "Şehir en fazla 100 karakter olabilir.")]
    [Display(Name = "Şehir")]
    public string? City { get; set; }

    [StringLength(100, ErrorMessage = "Ülke en fazla 100 karakter olabilir.")]
    [Display(Name = "Ülke")]
    public string? Country { get; set; }

    [StringLength(20, ErrorMessage = "Posta kodu en fazla 20 karakter olabilir.")]
    [Display(Name = "Posta Kodu")]
    public string? PostalCode { get; set; }

    [Url(ErrorMessage = "Geçerli bir web sitesi adresi giriniz.")]
    [StringLength(500, ErrorMessage = "Web sitesi adresi en fazla 500 karakter olabilir.")]
    [Display(Name = "Web Sitesi")]
    public string? Website { get; set; }

    // System Settings
    [Display(Name = "PMO Entegrasyonu")]
    public bool RequiresPMOIntegration { get; set; } = false;

    [Display(Name = "Ticket'ları Otomatik Onayla")]
    public bool AutoApproveTickets { get; set; } = false;

    [Display(Name = "E-posta Bildirimleri Gönder")]
    public bool SendEmailNotifications { get; set; } = true;

    [Display(Name = "Dosya Eklenmesine İzin Ver")]
    public bool AllowFileAttachments { get; set; } = true;

    [Range(1, 100, ErrorMessage = "Maksimum dosya boyutu 1-100 MB arasında olmalıdır.")]
    [Display(Name = "Maksimum Dosya Boyutu (MB)")]
    public int MaxFileSize { get; set; } = 10;

    [Url(ErrorMessage = "Geçerli bir API endpoint adresi giriniz.")]
    [StringLength(1000, ErrorMessage = "API endpoint en fazla 1000 karakter olabilir.")]
    [Display(Name = "PMO API Endpoint")]
    public string? PMOApiEndpoint { get; set; }

    [StringLength(500, ErrorMessage = "API key en fazla 500 karakter olabilir.")]
    [Display(Name = "PMO API Key")]
    public string? PMOApiKey { get; set; }
}