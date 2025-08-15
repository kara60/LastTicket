using System.ComponentModel.DataAnnotations;
using TicketSystem.Application.Features.Common.DTOs;

namespace TicketSystem.Web.Areas.Customer.Models;

// Step 1: Ticket Type Selection
public class CreateTicketStep1ViewModel
{
    public List<TicketTypeDto> TicketTypes { get; set; } = new();
}