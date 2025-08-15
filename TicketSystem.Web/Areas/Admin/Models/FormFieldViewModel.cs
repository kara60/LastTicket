using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Web.Areas.Admin.Models
{
    public class FormFieldViewModel
    {
        [Required(ErrorMessage = "Alan adı gereklidir.")]
        [Display(Name = "Alan Adı")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Etiket gereklidir.")]
        [Display(Name = "Etiket")]
        public string Label { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tip seçilmelidir.")]
        [Display(Name = "Tip")]
        public string Type { get; set; } = "text";

        [Display(Name = "Zorunlu")]
        public bool Required { get; set; }

        [Display(Name = "Placeholder")]
        public string? Placeholder { get; set; }

        [Display(Name = "Seçenekler (virgülle ayırın)")]
        public string? Options { get; set; }

        [Display(Name = "Minimum Uzunluk")]
        public int? MinLength { get; set; }

        [Display(Name = "Maximum Uzunluk")]
        public int? MaxLength { get; set; }

        [Display(Name = "Minimum Değer")]
        public int? Min { get; set; }

        [Display(Name = "Maximum Değer")]
        public int? Max { get; set; }
    }
}
