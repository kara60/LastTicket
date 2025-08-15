using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Application.Features.Tickets.Commands.UpdateTicketStatus;
using TicketSystem.Application.Features.Tickets.Commands.AddComment;
using TicketSystem.Application.Features.Tickets.Queries.GetTickets;
using TicketSystem.Application.Features.Tickets.Queries.GetTicketById;
using TicketSystem.Domain.Enums;

namespace TicketSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class TicketsController : Controller
{
    private readonly IMediator _mediator;

    public TicketsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> Index(GetTicketsQuery query)
    {
        var result = await _mediator.Send(query);
        return View(result.Data);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var result = await _mediator.Send(new GetTicketByIdQuery(id));
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Errors.FirstOrDefault();
            return RedirectToAction("Index");
        }
        return View(result.Data);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(Guid ticketId, TicketStatus newStatus, string? comment, bool sendToPmo = false)
    {
        var command = new UpdateTicketStatusCommand
        {
            TicketId = ticketId,
            NewStatus = newStatus,
            Comment = comment,
            SendToPmo = sendToPmo
        };

        var result = await _mediator.Send(command);

        if (result.IsSuccess)
        {
            TempData["Success"] = "Ticket durumu başarıyla güncellendi.";
        }
        else
        {
            TempData["Error"] = result.Errors.FirstOrDefault();
        }

        return RedirectToAction("Details", new { id = ticketId });
    }

    [HttpPost]
    public async Task<IActionResult> AddComment(Guid ticketId, string content, bool isInternal = false)
    {
        var command = new AddTicketCommentCommand
        {
            TicketId = ticketId,
            Content = content,
            IsInternal = isInternal
        };

        var result = await _mediator.Send(command);

        if (result.IsSuccess)
        {
            TempData["Success"] = "Yorum başarıyla eklendi.";
        }
        else
        {
            TempData["Error"] = result.Errors.FirstOrDefault();
        }

        return RedirectToAction("Details", new { id = ticketId });
    }
}
