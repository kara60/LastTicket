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

        // Build filter expression
        Expression<Func<Ticket, bool>> predicate = x => x.CompanyId == _currentUserService.CompanyId;

        // Customer filter for admin users
        if (request.CustomerId.HasValue)
        {
            var originalPredicate = predicate;
            predicate = x => originalPredicate.Invoke(x) && x.CustomerId == request.CustomerId;
        }

        // Customer users can only see their own customer tickets
        if (_currentUserService.IsCustomer && _currentUserService.CustomerId.HasValue)
        {
            var originalPredicate = predicate;
            predicate = x => originalPredicate.Invoke(x) && x.CustomerId == _currentUserService.CustomerId;
        }

        // Other filters
        if (request.TypeId.HasValue)
        {
            var originalPredicate = predicate;
            predicate = x => originalPredicate.Invoke(x) && x.TypeId == request.TypeId;
        }

        if (request.CategoryId.HasValue)
        {
            var originalPredicate = predicate;
            predicate = x => originalPredicate.Invoke(x) && x.CategoryId == request.CategoryId;
        }

        if (request.Status.HasValue)
        {
            var originalPredicate = predicate;
            predicate = x => originalPredicate.Invoke(x) && x.Status == request.Status;
        }

        if (request.AssignedToId.HasValue)
        {
            var originalPredicate = predicate;
            predicate = x => originalPredicate.Invoke(x) && x.AssignedToUserId == request.AssignedToId;
        }

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var originalPredicate = predicate;
            predicate = x => originalPredicate.Invoke(x) &&
                           (x.Title.Contains(request.SearchTerm) ||
                            x.TicketNumber.Contains(request.SearchTerm) ||
                            (x.Description != null && x.Description.Contains(request.SearchTerm)));
        }

        if (request.CreatedFrom.HasValue)
        {
            var originalPredicate = predicate;
            predicate = x => originalPredicate.Invoke(x) && x.CreatedAt >= request.CreatedFrom;
        }

        if (request.CreatedTo.HasValue)
        {
            var originalPredicate = predicate;
            predicate = x => originalPredicate.Invoke(x) && x.CreatedAt <= request.CreatedTo;
        }

        // Order by expression
        Expression<Func<Ticket, object>> orderBy = request.SortBy.ToLower() switch
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