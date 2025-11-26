using AutoMapper;
using SurveyBot.API.Mapping.ValueResolvers;
using SurveyBot.Core.Constants;
using SurveyBot.Core.DTOs;
using SurveyBot.Core.DTOs.Question;
using SurveyBot.Core.Entities;
using SurveyBot.Core.Enums;
using SurveyBot.Core.ValueObjects;

namespace SurveyBot.API.Mapping;

/// <summary>
/// AutoMapper profile for Question entity and related DTOs.
/// </summary>
public class QuestionMappingProfile : Profile
{
    public QuestionMappingProfile()
    {
        // ===========================
        // NextQuestionDeterminant Mappings
        // ===========================

        // Forward mapping: Value Object -> DTO
        CreateMap<NextQuestionDeterminant, NextQuestionDeterminantDto>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
            .ForMember(dest => dest.NextQuestionId, opt => opt.MapFrom(src => src.NextQuestionId));

        // Reverse mapping: DTO -> Value Object
        // Uses factory methods because NextQuestionDeterminant is a value object with private constructor
        CreateMap<NextQuestionDeterminantDto, NextQuestionDeterminant>()
            .ConstructUsing(dto =>
                dto.Type == NextStepType.GoToQuestion && dto.NextQuestionId.HasValue
                    ? NextQuestionDeterminant.ToQuestion(dto.NextQuestionId.Value)
                    : NextQuestionDeterminant.End()
            );

        // ===========================
        // QuestionOption Mappings
        // ===========================

        // QuestionOption -> QuestionOptionDto (Entity to DTO for option details)
        CreateMap<QuestionOption, QuestionOptionDto>();

        // Question -> QuestionDto (Entity to DTO for reading)
        CreateMap<Question, QuestionDto>()
            .ForMember(dest => dest.Options,
                opt => opt.MapFrom<QuestionOptionsResolver>())
            .ForMember(dest => dest.OptionDetails,
                opt => opt.MapFrom(src => src.Options))
            .ForMember(dest => dest.MediaContent,
                opt => opt.MapFrom<QuestionMediaContentResolver>())
            // TEMPORARY: Commented for migration generation (INFRA-002)
            // .ForMember(dest => dest.DefaultNextQuestionId,
            //     opt => opt.MapFrom(src => src.DefaultNextQuestionId))
            .ForMember(dest => dest.SupportsBranching,
                opt => opt.MapFrom(src => src.SupportsBranching));

        // CreateQuestionDto -> Question (DTO to Entity for creation)
        CreateMap<CreateQuestionDto, Question>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.SurveyId, opt => opt.Ignore()) // Will be set by service
            .ForMember(dest => dest.OrderIndex, opt => opt.Ignore()) // Will be calculated by service
            .ForMember(dest => dest.OptionsJson,
                opt => opt.MapFrom<QuestionOptionsJsonResolver>())
            .ForMember(dest => dest.MediaContent,
                opt => opt.MapFrom<QuestionMediaContentJsonResolver>())
            // TEMPORARY: Commented for migration generation (INFRA-002)
            // .ForMember(dest => dest.DefaultNextQuestionId, opt => opt.Ignore()) // Handled by service
            .ForMember(dest => dest.Options, opt => opt.Ignore()) // Handled by service
            // TEMPORARY: Commented for migration generation (INFRA-002)
            // .ForMember(dest => dest.DefaultNextQuestion, opt => opt.Ignore())
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
            .ForMember(dest => dest.MediaContent,
                opt => opt.MapFrom<UpdateQuestionMediaContentJsonResolver>())
            .ForMember(dest => dest.Survey, opt => opt.Ignore())
            .ForMember(dest => dest.Answers, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        // Question -> ConditionalFlowDto (Entity to DTO for conditional flow configuration)
        CreateMap<Question, ConditionalFlowDto>()
            .ForMember(dest => dest.QuestionId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.SupportsBranching, opt => opt.MapFrom(src => src.SupportsBranching))
            .ForMember(dest => dest.DefaultNext, opt => opt.MapFrom(src =>
                src.DefaultNext != null
                    ? new NextQuestionDeterminantDto
                    {
                        Type = src.DefaultNext.Type,
                        NextQuestionId = src.DefaultNext.NextQuestionId
                    }
                    : null))
            .ForMember(dest => dest.OptionFlows, opt => opt.MapFrom(src =>
                src.Options != null && src.Options.Any()
                    ? src.Options
                        .OrderBy(o => o.OrderIndex)
                        .Select(o => new OptionFlowDto
                        {
                            OptionId = o.Id,
                            OptionText = o.Text,
                            Next = o.Next != null
                                ? new NextQuestionDeterminantDto
                                {
                                    Type = o.Next.Type,
                                    NextQuestionId = o.Next.NextQuestionId
                                }
                                : new NextQuestionDeterminantDto
                                {
                                    Type = Core.Enums.NextStepType.GoToQuestion,
                                    NextQuestionId = null
                                }
                        })
                        .ToList()
                    : new List<OptionFlowDto>()));

        // QuestionOption -> OptionFlowDto (Entity to DTO for option-specific flow)
        CreateMap<QuestionOption, OptionFlowDto>()
            .ForMember(dest => dest.OptionId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.OptionText, opt => opt.MapFrom(src => src.Text))
            .ForMember(dest => dest.Next, opt => opt.MapFrom(src =>
                src.Next != null
                    ? new NextQuestionDeterminantDto
                    {
                        Type = src.Next.Type,
                        NextQuestionId = src.Next.NextQuestionId
                    }
                    : new NextQuestionDeterminantDto
                    {
                        Type = Core.Enums.NextStepType.GoToQuestion,
                        NextQuestionId = null
                    }));
    }
}
