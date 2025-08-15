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
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email.Value));

        CreateMap<Customer, CustomerDto>();

        // Deserialize işlemini handler’da yapacağız
        CreateMap<TicketType, TicketTypeDto>()
            .ForMember(dest => dest.FormDefinition, opt => opt.Ignore());

        CreateMap<TicketCategory, TicketCategoryDto>();
        CreateMap<TicketCategoryModule, TicketCategoryModuleDto>();

        // Deserialize işlemini handler’da yapacağız
        CreateMap<Ticket, TicketDto>()
            .ForMember(dest => dest.StatusDisplay, opt => opt.MapFrom(src => GetStatusDisplay(src.Status)))
            .ForMember(dest => dest.FormData, opt => opt.Ignore());

        CreateMap<TicketComment, TicketCommentDto>();
        CreateMap<TicketAttachment, TicketAttachmentDto>();
    }

    private static string GetStatusDisplay(TicketStatus status)
    {
        return status switch
        {
            TicketStatus.İnceleniyor => "İnceleniyor",
            TicketStatus.İşlemde => "İşlemde",
            TicketStatus.Çözüldü => "Çözüldü",
            TicketStatus.Kapandı => "Kapandı",
            TicketStatus.Reddedildi => "Reddedildi",
            _ => status.ToString()
        };
    }
}