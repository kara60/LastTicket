using System.ComponentModel.DataAnnotations;
using TicketSystem.Domain.Enums;

namespace TicketSystem.Web.Areas.Admin.Models;

public class CreateUserViewModel
{
    [Required(ErrorMessage = "Ad gereklidir.")]
    [StringLength(100, ErrorMessage = "Ad en fazla 100 karakter olabilir.")]
    [Display(Name = "Ad")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Soyad gereklidir.")]
    [StringLength(100, ErrorMessage = "Soyad en fazla 100 karakter olabilir.")]
    [Display(Name = "Soyad")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta gereklidir.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    [StringLength(200, ErrorMessage = "E-posta en fazla 200 karakter olabilir.")]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kullanıcı adı gereklidir.")]
    [StringLength(100, ErrorMessage = "Kullanıcı adı en fazla 100 karakter olabilir.")]
    [Display(Name = "Kullanıcı Adı")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre gereklidir.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Rol seçimi gereklidir.")]
    [Display(Name = "Rol")]
    public UserRole Role { get; set; }

    [Display(Name = "Müşteri")]
    public int? CustomerId { get; set; }

    public string FullName => $"{FirstName} {LastName}";
}