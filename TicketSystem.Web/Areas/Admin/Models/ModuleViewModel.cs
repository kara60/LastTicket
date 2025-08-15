using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Web.Areas.Admin.Models
{
    public class ModuleViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Modül adı gereklidir.")]
        [Display(Name = "Modül Adı")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Açıklama")]
        public string? Description { get; set; }

        [Display(Name = "Aktif")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Sıralama")]
        public int DisplayOrder { get; set; }
    }
}
