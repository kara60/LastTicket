using TicketSystem.Application.Common.Handlers;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Application.Common.Models;
using TicketSystem.Application.Features.Tickets.DTOs;
using TicketSystem.Domain.Entities;
using System.Linq.Expressions;

namespace TicketSystem.Application.Features.Tickets.Queries.GetMyTickets;

public class GetMyTicketsQueryHandler : IQueryHandler<GetMyTicketsQuery, PaginatedList<TicketListDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _current;

    public GetMyTicketsQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService current)
    {
        _unitOfWork = unitOfWork;
        _current = current;
    }

    public async Task<Result<PaginatedList<TicketListDto>>> Handle(GetMyTicketsQuery request, CancellationToken cancellationToken)
    {
        if (!_current.IsAuthenticated || !_current.CompanyId.HasValue || !_current.UserId.HasValue)
            return Result<PaginatedList<TicketListDto>>.Failure("Kullanıcı doğrulanamadı.");

        var filters = new List<Expression<Func<Ticket, bool>>>
        {
            x => x.CompanyId == _current.CompanyId
        };

        // Own tickets (created by) or within same customer
        if (_current.CustomerId.HasValue)
            filters.Add(x => x.CustomerId == _current.CustomerId);
        else
            filters.Add(x => x.CreatedByUserId == _current.UserId);

        if (!string.IsNullOrEmpty(request.SearchTerm))
            filters.Add(x => x.Title.Contains(request.SearchTerm!) || x.TicketNumber.Contains(request.SearchTerm!));

        if (request.TypeId.HasValue) filters.Add(x => x.TypeId == request.TypeId);
        if (request.CategoryId.HasValue) filters.Add(x => x.CategoryId == request.CategoryId);
        if (request.Status.HasValue) filters.Add(x => x.Status == request.Status);
        if (request.CreatedFrom.HasValue) filters.Add(x => x.CreatedAt >= request.CreatedFrom);
        if (request.CreatedTo.HasValue) filters.Add(x => x.CreatedAt <= request.CreatedTo);

        var predicate = CombineAnd(filters);

        Expression<Func<Ticket, object>> orderBy = request.SortBy?.ToLower() switch
        {
            "title" => x => x.Title,
            "status" => x => x.Status,
            "type" => x => x.Type.Name,
            "category" => x => x.Category.Name,
            _ => x => x.CreatedAt
        };

        var (tickets, totalCount) = await _unitOfWork.Tickets.GetPagedAsync(
            request.Page,
            request.PageSize,
            predicate,
            orderBy,
            !request.SortDescending,
            x => x.Type,
            x => x.Category,
            x => x.Customer!,
            x => x.CreatedBy,
            x => x.AssignedTo!,
            x => x.Comments,
            x => x.Attachments
        );

        var items = tickets.Select(t => new TicketListDto
        {
            Id = t.Id,
            TicketNumber = t.TicketNumber,
            Title = t.Title,
            StatusDisplay = t.Status.ToString(),
            TypeName = t.Type.Name,
            TypeColor = t.Type.Color,
            CategoryName = t.Category.Name,
            CustomerName = t.Customer?.Name ?? "",
            CreatedByName = $"{t.CreatedBy.FirstName} {t.CreatedBy.LastName}",
            AssignedToName = t.AssignedTo != null ? $"{t.AssignedTo.FirstName} {t.AssignedTo.LastName}" : null,
            CreatedAt = t.CreatedAt,
            CommentCount = t.Comments.Count,
            HasAttachments = t.Attachments.Any()
        }).ToList();

        return Result<PaginatedList<TicketListDto>>.Success(new PaginatedList<TicketListDto>(items, totalCount, request.Page, request.PageSize));
    }

    private static Expression<Func<Ticket, bool>> CombineAnd(IEnumerable<Expression<Func<Ticket, bool>>> expressions)
    {
        var param = Expression.Parameter(typeof(Ticket), "x");
        Expression body = Expression.Constant(true);

        foreach (var expr in expressions)
        {
            var replaced = new ParameterReplacer(param).Visit(expr.Body)!;
            body = Expression.AndAlso(body, replaced);
        }

        return Expression.Lambda<Func<Ticket, bool>>(body, param);
    }

    private sealed class ParameterReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression _parameter;
        public ParameterReplacer(ParameterExpression parameter) => _parameter = parameter;
        protected override Expression VisitParameter(ParameterExpression node) => _parameter;
    }
}