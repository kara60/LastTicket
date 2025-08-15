using AutoMapper;
using TicketSystem.Application.Common.Handlers;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Application.Common.Models;
using TicketSystem.Application.Features.Tickets.DTOs;
using System.Linq.Expressions;
using TicketSystem.Domain.Entities;

namespace TicketSystem.Application.Features.Tickets.Queries.GetTickets;

public class GetTicketsQueryHandler : IQueryHandler<GetTicketsQuery, PaginatedList<TicketListDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetTicketsQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<Result<PaginatedList<TicketListDto>>> Handle(GetTicketsQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.CompanyId.HasValue)
        {
            return Result<PaginatedList<TicketListDto>>.Failure("Kullanıcı doğrulanamadı.");
        }

        var filters = new List<Expression<Func<Ticket, bool>>>
        {
            x => x.CompanyId == _currentUserService.CompanyId
        };

        if (request.CustomerId.HasValue)
            filters.Add(x => x.CustomerId == request.CustomerId);

        if (_currentUserService.IsCustomer && _currentUserService.CustomerId.HasValue)
            filters.Add(x => x.CustomerId == _currentUserService.CustomerId);

        if (request.TypeId.HasValue)
            filters.Add(x => x.TypeId == request.TypeId);

        if (request.CategoryId.HasValue)
            filters.Add(x => x.CategoryId == request.CategoryId);

        if (request.Status.HasValue)
            filters.Add(x => x.Status == request.Status);

        if (request.AssignedToId.HasValue)
            filters.Add(x => x.AssignedToUserId == request.AssignedToId);

        if (!string.IsNullOrEmpty(request.SearchTerm))
            filters.Add(x =>
                x.Title.Contains(request.SearchTerm!) ||
                x.TicketNumber.Contains(request.SearchTerm!) ||
                (x.Description != null && x.Description.Contains(request.SearchTerm!)));

        if (request.CreatedFrom.HasValue)
            filters.Add(x => x.CreatedAt >= request.CreatedFrom);

        if (request.CreatedTo.HasValue)
            filters.Add(x => x.CreatedAt <= request.CreatedTo);

        var predicate = CombineAnd(filters);

        Expression<Func<Ticket, object>> orderBy = request.SortBy?.ToLower() switch
        {
            "title" => x => x.Title,
            "status" => x => x.Status,
            "customer" => x => x.Customer!.Name,
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

        var ticketDtos = tickets.Select(ticket => new TicketListDto
        {
            Id = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            Title = ticket.Title,
            StatusDisplay = GetStatusDisplay(ticket.Status),
            TypeName = ticket.Type.Name,
            TypeColor = ticket.Type.Color,
            CategoryName = ticket.Category.Name,
            CustomerName = ticket.Customer?.Name ?? "",
            CreatedByName = $"{ticket.CreatedBy.FirstName} {ticket.CreatedBy.LastName}",
            AssignedToName = ticket.AssignedTo != null ? $"{ticket.AssignedTo.FirstName} {ticket.AssignedTo.LastName}" : null,
            CreatedAt = ticket.CreatedAt,
            CommentCount = ticket.Comments.Count,
            HasAttachments = ticket.Attachments.Any()
        }).ToList();

        var result = new PaginatedList<TicketListDto>(ticketDtos, totalCount, request.Page, request.PageSize);
        return Result<PaginatedList<TicketListDto>>.Success(result);
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

    private static string GetStatusDisplay(Domain.Enums.TicketStatus status)
    {
        return status switch
        {
            Domain.Enums.TicketStatus.İnceleniyor => "İnceleniyor",
            Domain.Enums.TicketStatus.İşlemde => "İşlemde",
            Domain.Enums.TicketStatus.Çözüldü => "Çözüldü",
            Domain.Enums.TicketStatus.Kapandı => "Kapandı",
            Domain.Enums.TicketStatus.Reddedildi => "Reddedildi",
            _ => status.ToString()
        };
    }
}