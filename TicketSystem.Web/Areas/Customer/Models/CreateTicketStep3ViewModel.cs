using System.ComponentModel.DataAnnotations;
using TicketSystem.Application.Features.Common.DTOs;

namespace TicketSystem.Web.Areas.Customer.Models;

// Step 1: Ticket Type Selection
public class CreateTicketStep3ViewModel
{
    public int SelectedTypeId { get; set; }
    public int SelectedCategoryId { get; set; }
    public TicketTypeDto? SelectedType { get; set; }
    public TicketCategoryDto? SelectedCategory { get; set; }

    [Required(ErrorMessage = "Başlık gereklidir.")]
    [Display(Name = "Başlık")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Display(Name = "Modül")]
    public string? SelectedModule { get; set; }

    public Dictionary<string, object> FormData { get; set; } = new();
}