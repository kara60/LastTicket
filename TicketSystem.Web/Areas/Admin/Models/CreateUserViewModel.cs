using System.ComponentModel.DataAnnotations;
using TicketSystem.Domain.Enums;

namespace TicketSystem.Web.Areas.Admin.Models;

public class CreateUserViewModel
{
    [Required(ErrorMessage = "Ad gereklidir.")]
    [Display(Name = "Ad")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Soyad gereklidir.")]
    [Display(Name = "Soyad")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta gereklidir.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kullanıcı adı gereklidir.")]
    [Display(Name = "Kullanıcı Adı")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre gereklidir.")]
    [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Rol seçilmelidir.")]
    [Display(Name = "Rol")]
    public UserRole Role { get; set; }

    [Display(Name = "Müşteri")]
    public Guid? CustomerId { get; set; }
}