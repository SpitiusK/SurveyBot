using AutoMapper;
using SurveyBot.API.Mapping.ValueResolvers;
using SurveyBot.Core.DTOs.Response;
using SurveyBot.Core.Entities;

namespace SurveyBot.API.Mapping;

/// <summary>
/// AutoMapper profile for Response entity and related DTOs.
/// </summary>
public class ResponseMappingProfile : Profile
{
    public ResponseMappingProfile()
    {
        // Response -> ResponseDto (Entity to DTO for reading)
        CreateMap<Response, ResponseDto>()
            .ForMember(dest => dest.RespondentUsername,
                opt => opt.Ignore()) // This would come from a separate User lookup if needed
            .ForMember(dest => dest.RespondentFirstName,
                opt => opt.Ignore()) // This would come from a separate User lookup if needed
            .ForMember(dest => dest.AnsweredCount,
                opt => opt.MapFrom<ResponseAnsweredCountResolver>())
            .ForMember(dest => dest.TotalQuestions,
                opt => opt.MapFrom<ResponseTotalQuestionsResolver>());

        // Response -> ResponseListDto (Entity to DTO for list view)
        CreateMap<Response, ResponseListDto>()
            .ForMember(dest => dest.RespondentUsername,
                opt => opt.Ignore()) // This would come from a separate User lookup if needed
            .ForMember(dest => dest.RespondentFirstName,
                opt => opt.Ignore()) // This would come from a separate User lookup if needed
            .ForMember(dest => dest.AnsweredCount,
                opt => opt.MapFrom<ResponseAnsweredCountResolver>())
            .ForMember(dest => dest.TotalQuestions,
                opt => opt.MapFrom<ResponseTotalQuestionsResolver>());

        // CreateResponseDto -> Response (DTO to Entity for creation)
        CreateMap<CreateResponseDto, Response>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.SurveyId, opt => opt.Ignore()) // Will be set by service
            .ForMember(dest => dest.IsComplete, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.StartedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.SubmittedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Survey, opt => opt.Ignore())
            .ForMember(dest => dest.Answers, opt => opt.Ignore());
    }
}
