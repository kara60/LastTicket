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

        // UnitOfWork ile include'ları kullan
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

        // Comments için User'ları ayrıca yükle
        if (ticket.Comments.Any())
        {
            var commentUserIds = ticket.Comments.Select(c => c.UserId).Distinct().ToList();
            var commentUsers = await _unitOfWork.Users.FindAsync(u => commentUserIds.Contains(u.Id));
            var userDict = commentUsers.ToDictionary(u => u.Id, u => u);

            foreach (var comment in ticket.Comments)
            {
                if (userDict.TryGetValue(comment.UserId, out var user))
                {
                    comment.User = user; // Navigation property'yi set et
                }
            }
        }

        var dto = _mapper.Map<TicketDto>(ticket);

        // ✅ ENHANCED FormData parse with detailed logging and error handling
        try
        {
            Console.WriteLine($"=== FormData DESERIALIZATION DEBUG ===");
            Console.WriteLine($"Raw FormData from DB: '{ticket.FormData}'");
            Console.WriteLine($"FormData length: {ticket.FormData?.Length ?? 0}");

            if (string.IsNullOrEmpty(ticket.FormData))
            {
                Console.WriteLine("FormData is null or empty, setting empty dictionary");
                dto.FormData = new Dictionary<string, object>();
            }
            else
            {
                try
                {
                    // Try to deserialize as Dictionary<string, object>
                    var deserializedData = JsonSerializer.Deserialize<Dictionary<string, object>>(ticket.FormData);

                    if (deserializedData != null && deserializedData.Any())
                    {
                        dto.FormData = deserializedData;
                        Console.WriteLine($"Successfully deserialized {deserializedData.Count} FormData entries:");

                        foreach (var kvp in deserializedData)
                        {
                            Console.WriteLine($"  FormData[{kvp.Key}] = '{kvp.Value}' (Type: {kvp.Value?.GetType().Name ?? "null"})");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Deserialized FormData is null or empty, setting empty dictionary");
                        dto.FormData = new Dictionary<string, object>();
                    }
                }
                catch (JsonException jsonEx)
                {
                    Console.WriteLine($"JSON deserialization failed: {jsonEx.Message}");

                    // Fallback: Try to deserialize as Dictionary<string, JsonElement>
                    try
                    {
                        var elementDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(ticket.FormData);
                        if (elementDict != null)
                        {
                            dto.FormData = new Dictionary<string, object>();
                            foreach (var kvp in elementDict)
                            {
                                // Convert JsonElement to appropriate type
                                object value = kvp.Value.ValueKind switch
                                {
                                    JsonValueKind.String => kvp.Value.GetString() ?? "",
                                    JsonValueKind.Number => kvp.Value.TryGetInt32(out var intVal) ? intVal : kvp.Value.GetDouble(),
                                    JsonValueKind.True => true,
                                    JsonValueKind.False => false,
                                    JsonValueKind.Null => "",
                                    _ => kvp.Value.ToString()
                                };

                                dto.FormData[kvp.Key] = value;
                                Console.WriteLine($"  Fallback FormData[{kvp.Key}] = '{value}' (Type: {value?.GetType().Name ?? "null"})");
                            }

                            Console.WriteLine($"Fallback deserialization successful: {dto.FormData.Count} entries");
                        }
                        else
                        {
                            Console.WriteLine("Fallback deserialization also failed, setting empty dictionary");
                            dto.FormData = new Dictionary<string, object>();
                        }
                    }
                    catch (Exception fallbackEx)
                    {
                        Console.WriteLine($"Fallback deserialization also failed: {fallbackEx.Message}");
                        dto.FormData = new Dictionary<string, object>();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected deserialization error: {ex.Message}");
                    dto.FormData = new Dictionary<string, object>();
                }
            }

            Console.WriteLine($"Final DTO FormData count: {dto.FormData.Count}");
            Console.WriteLine($"=== FormData DESERIALIZATION END ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Critical error in FormData processing: {ex.Message}");
            dto.FormData = new Dictionary<string, object>();
        }

        return Result<TicketDto>.Success(dto);
    }
}