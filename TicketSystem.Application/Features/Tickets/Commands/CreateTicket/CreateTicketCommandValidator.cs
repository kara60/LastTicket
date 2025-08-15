using FluentValidation;

namespace TicketSystem.Application.Features.Tickets.Commands.CreateTicket;

public class CreateTicketCommandValidator : AbstractValidator<CreateTicketCommand>
{
    public CreateTicketCommandValidator()
    {
        RuleFor(x => x.TypeId)
            .NotEmpty().WithMessage("Ticket türü seçilmelidir.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Ticket kategorisi seçilmelidir.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Başlık gereklidir.")
            .MaximumLength(200).WithMessage("Başlık en fazla 200 karakter olabilir.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Açıklama en fazla 2000 karakter olabilir.");
    }
}