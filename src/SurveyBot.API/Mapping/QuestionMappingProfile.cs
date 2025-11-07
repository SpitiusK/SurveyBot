using AutoMapper;
using SurveyBot.API.Mapping.ValueResolvers;
using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.Entities;

namespace SurveyBot.API.Mapping;

/// <summary>
/// AutoMapper profile for Question entity and related DTOs.
/// </summary>
public class QuestionMappingProfile : Profile
{
    public QuestionMappingProfile()
    {
        // Question -> QuestionDto (Entity to DTO for reading)
        CreateMap<Question, QuestionDto>()
            .ForMember(dest => dest.Options,
                opt => opt.MapFrom<QuestionOptionsResolver>());

        // CreateQuestionDto -> Question (DTO to Entity for creation)
        CreateMap<CreateQuestionDto, Question>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.SurveyId, opt => opt.Ignore()) // Will be set by service
            .ForMember(dest => dest.OrderIndex, opt => opt.Ignore()) // Will be calculated by service
            .ForMember(dest => dest.OptionsJson,
                opt => opt.MapFrom<QuestionOptionsJsonResolver>())
            .ForMember(dest => dest.Survey, opt => opt.Ignore())
            .ForMember(dest => dest.Answers, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        // UpdateQuestionDto -> Question (DTO to Entity for update)
        CreateMap<UpdateQuestionDto, Question>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.SurveyId, opt => opt.Ignore())
            .ForMember(dest => dest.OrderIndex, opt => opt.Ignore()) // Use ReorderQuestionsDto instead
            .ForMember(dest => dest.OptionsJson,
                opt => opt.MapFrom<UpdateQuestionOptionsJsonResolver>())
            .ForMember(dest => dest.Survey, opt => opt.Ignore())
            .ForMember(dest => dest.Answers, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
    }
}
