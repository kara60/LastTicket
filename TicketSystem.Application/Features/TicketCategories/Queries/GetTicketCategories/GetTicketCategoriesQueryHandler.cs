using TicketSystem.Application.Common.Handlers;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Application.Common.Models;
using TicketSystem.Application.Features.Common.DTOs;
using TicketSystem.Domain.Entities;

namespace TicketSystem.Application.Features.TicketCategories.Queries.GetTicketCategories;

public class GetTicketCategoriesQueryHandler : IQueryHandler<GetTicketCategoriesQuery, List<TicketCategoryDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _current;

    public GetTicketCategoriesQueryHandler(IUnitOfWork uow, ICurrentUserService current)
    {
        _uow = uow;
        _current = current;
    }

    public async Task<Result<List<TicketCategoryDto>>> Handle(GetTicketCategoriesQuery request, CancellationToken cancellationToken)
    {
        if (!_current.IsAuthenticated || !_current.CompanyId.HasValue)
            return Result<List<TicketCategoryDto>>.Failure("Kullanıcı doğrulanamadı.");

        IEnumerable<TicketCategory> cats;
        if (request.IncludeModules)
        {
            cats = await _uow.TicketCategories.FindAsync(
                x => x.CompanyId == _current.CompanyId && (!request.OnlyActive || x.IsActive),
                x => x.Modules);
        }
        else
        {
            cats = await _uow.TicketCategories.FindAsync(
                x => x.CompanyId == _current.CompanyId && (!request.OnlyActive || x.IsActive));
        }

        var list = cats
            .OrderBy(x => x.SortOrder)
            .Select(c => new TicketCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description ?? "",
                Icon = c.Icon ?? "",
                Color = c.Color,
                Modules = request.IncludeModules
                    ? c.Modules.Where(m => m.IsActive).OrderBy(m => m.SortOrder).Select(m => new TicketCategoryModuleDto
                    {
                        Id = m.Id,
                        Name = m.Name,
                        Description = m.Description
                    }).ToList()
                    : new List<TicketCategoryModuleDto>()
            }).ToList();

        return Result<List<TicketCategoryDto>>.Success(list);
    }
}