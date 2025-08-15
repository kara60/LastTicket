using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Web.Areas.Admin.Models
{
    public class EditCategoryViewModel : CreateCategoryViewModel
    {
        public Guid Id { get; set; }

        [Display(Name = "Aktif")]
        public bool IsActive { get; set; } = true;

        public List<ModuleViewModel> Modules { get; set; } = new();
    }
}
