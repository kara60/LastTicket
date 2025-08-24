// Areas/Admin/Models/EditCustomerViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Web.Areas.Admin.Models;

public class EditCustomerViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Müşteri adı gereklidir.")]
    [StringLength(200, ErrorMessage = "Müşteri adı en fazla 200 karakter olabilir.")]
    [Display(Name = "Müşteri Adı")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "İletişim e-postası gereklidir.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    [StringLength(100, ErrorMessage = "E-posta en fazla 100 karakter olabilir.")]
    [Display(Name = "İletişim E-postası")]
    public string ContactEmail { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
    [StringLength(20, ErrorMessage = "Telefon numarası en fazla 20 karakter olabilir.")]
    [Display(Name = "İletişim Telefonu")]
    public string? ContactPhone { get; set; }

    [StringLength(100, ErrorMessage = "İletişim kişisi adı en fazla 100 karakter olabilir.")]
    [Display(Name = "İletişim Kişisi")]
    public string? ContactPerson { get; set; }

    [StringLength(500, ErrorMessage = "Adres en fazla 500 karakter olabilir.")]
    [Display(Name = "Adres")]
    public string? Address { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;
}