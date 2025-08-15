using AutoMapper;
using TicketSystem.Application.Common.Handlers;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Application.Common.Models;
using TicketSystem.Application.Features.Tickets.DTOs;
using TicketSystem.Domain.Entities;
using System.Text.Json;

namespace TicketSystem.Application.Features.Tickets.Queries.GetTicketById;

public class GetTicketByIdQueryHandler : IQueryHandler<GetTicketByIdQuery, TicketDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;

    public GetTicketByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUser = currentUser;
    }

    public async Task<Result<TicketDto>> Handle(GetTicketByIdQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.CompanyId.HasValue)
            return Result<TicketDto>.Failure("Kullanıcı doğrulanamadı.");

        var ticket = await _unitOfWork.Tickets.FirstOrDefaultAsync(
            x => x.Id == request.Id && x.CompanyId == _currentUser.CompanyId,
            x => x.Type,
            x => x.Category,
            x => x.Customer!,
            x => x.CreatedBy,
            x => x.AssignedTo!,
            x => x.Comments,
            x => x.Attachments
        );

        if (ticket == null)
            return Result<TicketDto>.Failure("Ticket bulunamadı.");

        var dto = _mapper.Map<TicketDto>(ticket);

        // FormData parse (AutoMapper’da Ignore edildi)
        dto.FormData = string.IsNullOrEmpty(ticket.FormData)
            ? new Dictionary<string, object>()
            : (JsonSerializer.Deserialize<Dictionary<string, object>>(ticket.FormData!) ?? new());

        dto.Comments = ticket.Comments
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new TicketCommentDto
            {
                Id = c.Id,
                Content = c.Content,
                IsInternal = c.IsInternal,
                CreatedAt = c.CreatedAt,
                User = _mapper.Map<Application.Features.Common.DTOs.UserDto>(c.User)
            }).ToList();

        dto.Attachments = ticket.Attachments
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new TicketAttachmentDto
            {
                Id = a.Id,
                FileName = a.FileName,
                ContentType = a.ContentType,
                FileSize = a.FileSizeBytes,
                CreatedAt = a.CreatedAt,
            }).ToList();

        return Result<TicketDto>.Success(dto);
    }
}