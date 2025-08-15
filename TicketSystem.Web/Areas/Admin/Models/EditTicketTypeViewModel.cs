using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Web.Areas.Admin.Models
{
    public class EditTicketTypeViewModel : CreateTicketTypeViewModel
    {
        public Guid Id { get; set; }

        [Display(Name = "Aktif")]
        public bool IsActive { get; set; } = true;
    }
}
