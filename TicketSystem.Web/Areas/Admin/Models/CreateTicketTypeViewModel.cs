using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Web.Areas.Admin.Models;

public class CreateTicketTypeViewModel
{
    [Required(ErrorMessage = "Tür adı gereklidir.")]
    [Display(Name = "Tür Adı")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "İkon gereklidir.")]
    [Display(Name = "İkon")]
    public string Icon { get; set; } = "ticket-alt";

    [Required(ErrorMessage = "Renk gereklidir.")]
    [Display(Name = "Renk")]
    public string Color { get; set; } = "#3b82f6";

    [Display(Name = "Sıralama")]
    [Range(0, 999, ErrorMessage = "Sıralama 0-999 arasında olmalıdır.")]
    public int DisplayOrder { get; set; }

    public List<FormFieldViewModel> FormFields { get; set; } = new();
}