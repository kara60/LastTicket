using AutoMapper;
using TicketSystem.Application.Features.Common.DTOs;
using TicketSystem.Application.Features.Tickets.DTOs;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;

namespace TicketSystem.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()));

        CreateMap<Customer, CustomerDto>();

        CreateMap<TicketType, TicketTypeDto>()
            .ForMember(dest => dest.FormDefinition, opt => opt.MapFrom(src =>
                System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(src.FormDefinition ?? "{}", (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, object>()));

        CreateMap<TicketCategory, TicketCategoryDto>();

        CreateMap<TicketCategoryModule, TicketCategoryModuleDto>();

        CreateMap<Ticket, TicketDto>()
            .ForMember(dest => dest.StatusDisplay, opt => opt.MapFrom(src => GetStatusDisplay(src.Status)))
            .ForMember(dest => dest.FormData, opt => opt.MapFrom(src =>
                System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(System.Text.Json.JsonSerializer.Serialize(src.FormData), (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, object>()));

        CreateMap<Ticket, TicketListDto>()
            .ForMember(dest => dest.StatusDisplay, opt => opt.MapFrom(src => GetStatusDisplay(src.Status)))
            .ForMember(dest => dest.TypeName, opt => opt.MapFrom(src => src.Type.Name))
            .ForMember(dest => dest.TypeColor, opt => opt.MapFrom(src => src.Type.Color))
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name))
            .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer != null ? src.Customer.Name : ""))
            .ForMember(dest => dest.CreatedByName, opt => opt.MapFrom(src => $"{src.CreatedBy.FirstName} {src.CreatedBy.LastName}"))
            .ForMember(dest => dest.AssignedToName, opt => opt.MapFrom(src => src.AssignedTo != null ? $"{src.AssignedTo.FirstName} {src.AssignedTo.LastName}" : null))
            .ForMember(dest => dest.CommentCount, opt => opt.MapFrom(src => src.Comments.Count))
            .ForMember(dest => dest.HasAttachments, opt => opt.MapFrom(src => src.Attachments.Any()));

        CreateMap<TicketComment, TicketCommentDto>();

        CreateMap<TicketAttachment, TicketAttachmentDto>();
    }

    private static string GetStatusDisplay(TicketStatus status)
    {
        return status switch
        {
            TicketStatus.Inceleniyor => "İnceleniyor",
            TicketStatus.Islenmede => "İşlemde",
            TicketStatus.Cozuldu => "Çözüldü",
            TicketStatus.Kapatildi => "Kapatıldı",
            _ => status.ToString()
        };
    }
}