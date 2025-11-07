using AutoMapper;
using SurveyBot.Core.DTOs.User;
using SurveyBot.Core.Entities;

namespace SurveyBot.API.Mapping;

/// <summary>
/// AutoMapper profile for User entity and related DTOs.
/// </summary>
public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        // User -> UserDto (Entity to DTO for reading)
        CreateMap<User, UserDto>();

        // LoginDto -> User (DTO to Entity - for user lookup/creation)
        // Note: This mapping is primarily for creating users during Telegram authentication
        CreateMap<LoginDto, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Surveys, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
    }
}
