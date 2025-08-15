using System.ComponentModel.DataAnnotations;
using TicketSystem.Application.Features.Common.DTOs;

namespace TicketSystem.Web.Areas.Customer.Models;

// Step 1: Ticket Type Selection
public class CreateTicketStep4ViewModel
{
    public Guid SelectedTypeId { get; set; }
    public Guid SelectedCategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SelectedModule { get; set; }
    public Dictionary<string, object> FormData { get; set; } = new();
    public TicketTypeDto? SelectedType { get; set; }
    public TicketCategoryDto? SelectedCategory { get; set; }
}