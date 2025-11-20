using AutoMapper;
using SurveyBot.Core.DTOs.Branching;
using SurveyBot.Core.Entities;
using System.Text.Json;

namespace SurveyBot.API.Mapping;

/// <summary>
/// AutoMapper profile for QuestionBranchingRule entity and related DTOs.
/// </summary>
public class BranchingMappingProfile : Profile
{
    public BranchingMappingProfile()
    {
        // QuestionBranchingRule -> BranchingRuleDto (Entity to DTO for reading)
        CreateMap<QuestionBranchingRule, BranchingRuleDto>()
            .ForMember(dest => dest.Condition,
                opt => opt.MapFrom(src => DeserializeCondition(src.ConditionJson)));

        // CreateBranchingRuleDto -> QuestionBranchingRule (DTO to Entity for creation)
        CreateMap<CreateBranchingRuleDto, QuestionBranchingRule>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ConditionJson,
                opt => opt.MapFrom(src => SerializeCondition(src.Condition)))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.SourceQuestion, opt => opt.Ignore())
            .ForMember(dest => dest.TargetQuestion, opt => opt.Ignore());

        // UpdateBranchingRuleDto -> QuestionBranchingRule (DTO to Entity for update)
        CreateMap<UpdateBranchingRuleDto, QuestionBranchingRule>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.SourceQuestionId, opt => opt.Ignore()) // Cannot change source
            .ForMember(dest => dest.ConditionJson,
                opt => opt.MapFrom(src => SerializeCondition(src.Condition)))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.SourceQuestion, opt => opt.Ignore())
            .ForMember(dest => dest.TargetQuestion, opt => opt.Ignore());

        // BranchingConditionDto <-> BranchingCondition (bidirectional)
        CreateMap<BranchingConditionDto, BranchingCondition>().ReverseMap();
    }

    private static BranchingConditionDto? DeserializeCondition(string json)
    {
        return JsonSerializer.Deserialize<BranchingConditionDto>(json);
    }

    private static string SerializeCondition(BranchingConditionDto condition)
    {
        return JsonSerializer.Serialize(condition);
    }
}
