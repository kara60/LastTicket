using System.ComponentModel.DataAnnotations;
using TicketSystem.Application.Features.Common.DTOs;

namespace TicketSystem.Web.Areas.Customer.Models;

// Step 1: Ticket Type Selection
public class CreateTicketStep2ViewModel
{
    public Guid SelectedTypeId { get; set; }
    public TicketTypeDto? SelectedType { get; set; }
    public List<TicketCategoryDto> Categories { get; set; } = new();
}