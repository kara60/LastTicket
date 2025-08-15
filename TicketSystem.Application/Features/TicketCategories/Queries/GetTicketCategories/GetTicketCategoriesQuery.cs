using TicketSystem.Application.Common.Queries;
using TicketSystem.Application.Features.Common.DTOs;

namespace TicketSystem.Application.Features.TicketCategories.Queries.GetTicketCategories;

public class GetTicketCategoriesQuery : IQuery<List<TicketCategoryDto>>
{
    public bool OnlyActive { get; set; } = true;
    public bool IncludeModules { get; set; } = true;
}