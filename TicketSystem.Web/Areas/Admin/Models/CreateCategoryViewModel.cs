using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Web.Areas.Admin.Models
{

    public class CreateCategoryViewModel
    {
        [Required(ErrorMessage = "Kategori adı gereklidir.")]
        [Display(Name = "Kategori Adı")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Açıklama")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "İkon gereklidir.")]
        [Display(Name = "İkon")]
        public string Icon { get; set; } = "folder";

        [Required(ErrorMessage = "Renk gereklidir.")]
        [Display(Name = "Renk")]
        public string Color { get; set; } = "#6366f1";

        [Display(Name = "Sıralama")]
        [Range(0, 999, ErrorMessage = "Sıralama 0-999 arasında olmalıdır.")]
        public int DisplayOrder { get; set; }
    }
}
