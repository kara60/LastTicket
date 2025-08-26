using TicketSystem.Application.Common.Handlers;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Application.Common.Models;
using TicketSystem.Application.Features.Common.DTOs;

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

        // Include navigation property ile birlikte çek
        var categories = await _uow.TicketCategories.FindAsync(
            x => x.CompanyId == _current.CompanyId && (!request.OnlyActive || x.IsActive),
            x => x.Modules); // Include Modules navigation property

        var list = categories
            .OrderBy(x => x.SortOrder)
            .Select(x => new TicketCategoryDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description ?? "",
                Icon = x.Icon ?? "",
                Color = x.Color,
                IsActive = x.IsActive,
                SortOrder = x.SortOrder,
                Modules = x.Modules
                    .Where(m => m.IsActive) // Sadece aktif modüller
                    .OrderBy(m => m.SortOrder)
                    .Select(m => new TicketCategoryModuleDto
                    {
                        Id = m.Id,
                        Name = m.Name,
                        Description = m.Description ?? ""
                    }).ToList()
            })
            .ToList();

        return Result<List<TicketCategoryDto>>.Success(list);
    }
}