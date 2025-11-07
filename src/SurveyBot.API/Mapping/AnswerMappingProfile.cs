using AutoMapper;
using SurveyBot.API.Mapping.ValueResolvers;
using SurveyBot.Core.DTOs.Answer;
using SurveyBot.Core.Entities;

namespace SurveyBot.API.Mapping;

/// <summary>
/// AutoMapper profile for Answer entity and related DTOs.
/// </summary>
public class AnswerMappingProfile : Profile
{
    public AnswerMappingProfile()
    {
        // Answer -> AnswerDto (Entity to DTO for reading)
        CreateMap<Answer, AnswerDto>()
            .ForMember(dest => dest.QuestionText,
                opt => opt.MapFrom(src => src.Question != null ? src.Question.QuestionText : string.Empty))
            .ForMember(dest => dest.QuestionType,
                opt => opt.MapFrom(src => src.Question != null ? src.Question.QuestionType : QuestionType.Text))
            .ForMember(dest => dest.SelectedOptions,
                opt => opt.MapFrom<AnswerSelectedOptionsResolver>())
            .ForMember(dest => dest.RatingValue,
                opt => opt.MapFrom<AnswerRatingValueResolver>());

        // CreateAnswerDto -> Answer (DTO to Entity for creation)
        CreateMap<CreateAnswerDto, Answer>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ResponseId, opt => opt.Ignore()) // Will be set by service
            .ForMember(dest => dest.AnswerJson,
                opt => opt.MapFrom<AnswerJsonResolver>())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.Response, opt => opt.Ignore())
            .ForMember(dest => dest.Question, opt => opt.Ignore());
    }
}
