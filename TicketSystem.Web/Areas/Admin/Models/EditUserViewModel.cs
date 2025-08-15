using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;
using TicketSystem.Web.Areas.Admin.Models;

public class EditUserViewModel
{
    public Guid Id { get; set; }

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

    [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
    [DataType(DataType.Password)]
    [Display(Name = "Yeni Şifre (Boş bırakılırsa değişmez)")]
    public string? NewPassword { get; set; }

    [Required(ErrorMessage = "Rol seçilmelidir.")]
    [Display(Name = "Rol")]
    public UserRole Role { get; set; }

    [Display(Name = "Müşteri")]
    public Guid? CustomerId { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;
}