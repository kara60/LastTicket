// Areas/Admin/Models/ModuleViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Web.Areas.Admin.Models;

public class ModuleViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Modül adı gereklidir.")]
    [StringLength(100, ErrorMessage = "Modül adı en fazla 100 karakter olabilir.")]
    [Display(Name = "Modül Adı")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Display(Name = "Sıralama")]
    [Range(0, 999, ErrorMessage = "Sıralama 0-999 arasında olmalıdır.")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;
}