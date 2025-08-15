using TicketSystem.Application.Common.Handlers;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Application.Common.Models;
using TicketSystem.Application.Features.Common.DTOs;
using System.Text.Json;
using TicketSystem.Domain.Entities;

namespace TicketSystem.Application.Features.TicketTypes.Queries.GetTicketTypes;

public class GetTicketTypesQueryHandler : IQueryHandler<GetTicketTypesQuery, List<TicketTypeDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _current;

    public GetTicketTypesQueryHandler(IUnitOfWork uow, ICurrentUserService current)
    {
        _uow = uow;
        _current = current;
    }

    public async Task<Result<List<TicketTypeDto>>> Handle(GetTicketTypesQuery request, CancellationToken cancellationToken)
    {
        if (!_current.IsAuthenticated || !_current.CompanyId.HasValue)
            return Result<List<TicketTypeDto>>.Failure("Kullanıcı doğrulanamadı.");

        var types = await _uow.TicketTypes.FindAsync(x =>
            x.CompanyId == _current.CompanyId &&
            (!request.OnlyActive || x.IsActive));

        var list = types
            .OrderBy(x => x.SortOrder)
            .Select(x => new TicketTypeDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description ?? "",
                Icon = x.Icon ?? "",
                Color = x.Color,
                FormDefinition = string.IsNullOrWhiteSpace(x.FormDefinition)
                    ? new Dictionary<string, object>()
                    : (JsonSerializer.Deserialize<Dictionary<string, object>>(x.FormDefinition!) ?? new())
            })
            .ToList();

        return Result<List<TicketTypeDto>>.Success(list);
    }
}