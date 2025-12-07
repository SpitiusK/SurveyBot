using AutoMapper;
using SurveyBot.Core.DTOs.Answer;
using SurveyBot.Core.Entities;
using SurveyBot.Core.ValueObjects.Answers;

namespace SurveyBot.API.Mapping;

/// <summary>
/// AutoMapper profile for Answer entity and related DTOs.
/// Simplified in v1.5.0 to use answer.Value pattern matching instead of JSON parsing.
/// </summary>
public class AnswerMappingProfile : Profile
{
    public AnswerMappingProfile()
    {
        // Answer -> AnswerDto (Entity to DTO for reading)
        // Uses pattern matching on answer.Value for type-safe extraction
        CreateMap<Answer, AnswerDto>()
            .ForMember(dest => dest.QuestionText,
                opt => opt.MapFrom(src => src.Question != null ? src.Question.QuestionText : string.Empty))
            .ForMember(dest => dest.QuestionType,
                opt => opt.MapFrom(src => src.Question != null ? src.Question.QuestionType : QuestionType.Text))
            // Properties set in AfterMap - ignore for configuration validation
            .ForMember(dest => dest.AnswerText, opt => opt.Ignore())
            .ForMember(dest => dest.SelectedOptions, opt => opt.Ignore())
            .ForMember(dest => dest.RatingValue, opt => opt.Ignore())
            .ForMember(dest => dest.Latitude, opt => opt.Ignore())
            .ForMember(dest => dest.Longitude, opt => opt.Ignore())
            .ForMember(dest => dest.LocationAccuracy, opt => opt.Ignore())
            .ForMember(dest => dest.LocationTimestamp, opt => opt.Ignore())
            .ForMember(dest => dest.NumberValue, opt => opt.Ignore())
            .ForMember(dest => dest.DateValue, opt => opt.Ignore())
            .ForMember(dest => dest.DisplayValue, opt => opt.Ignore())
            // Use AfterMap for pattern matching on Value - much cleaner than separate resolvers
            .AfterMap((src, dest) =>
            {
                // Pattern match on answer.Value for type-safe property extraction
                switch (src.Value)
                {
                    case TextAnswerValue textValue:
                        dest.AnswerText = textValue.Text;
                        dest.SelectedOptions = null;
                        dest.RatingValue = null;
                        dest.Latitude = null;
                        dest.Longitude = null;
                        dest.LocationAccuracy = null;
                        dest.LocationTimestamp = null;
                        dest.NumberValue = null;
                        dest.DateValue = null;
                        break;

                    case SingleChoiceAnswerValue singleChoice:
                        dest.AnswerText = null;
                        dest.SelectedOptions = new List<string> { singleChoice.SelectedOption };
                        dest.RatingValue = null;
                        dest.Latitude = null;
                        dest.Longitude = null;
                        dest.LocationAccuracy = null;
                        dest.LocationTimestamp = null;
                        dest.NumberValue = null;
                        dest.DateValue = null;
                        break;

                    case MultipleChoiceAnswerValue multipleChoice:
                        dest.AnswerText = null;
                        dest.SelectedOptions = multipleChoice.SelectedOptions.ToList();
                        dest.RatingValue = null;
                        dest.Latitude = null;
                        dest.Longitude = null;
                        dest.LocationAccuracy = null;
                        dest.LocationTimestamp = null;
                        dest.NumberValue = null;
                        dest.DateValue = null;
                        break;

                    case RatingAnswerValue ratingValue:
                        dest.AnswerText = null;
                        dest.SelectedOptions = null;
                        dest.RatingValue = ratingValue.Rating;
                        dest.Latitude = null;
                        dest.Longitude = null;
                        dest.LocationAccuracy = null;
                        dest.LocationTimestamp = null;
                        dest.NumberValue = null;
                        dest.DateValue = null;
                        break;

                    case LocationAnswerValue locationValue:
                        dest.AnswerText = null;
                        dest.SelectedOptions = null;
                        dest.RatingValue = null;
                        dest.Latitude = locationValue.Latitude;
                        dest.Longitude = locationValue.Longitude;
                        dest.LocationAccuracy = locationValue.Accuracy;
                        dest.LocationTimestamp = locationValue.Timestamp;
                        dest.NumberValue = null;
                        dest.DateValue = null;
                        break;

                    case NumberAnswerValue numberValue:
                        dest.NumberValue = numberValue.Value;
                        dest.AnswerText = null;
                        dest.SelectedOptions = null;
                        dest.RatingValue = null;
                        dest.Latitude = null;
                        dest.Longitude = null;
                        dest.LocationAccuracy = null;
                        dest.LocationTimestamp = null;
                        dest.DateValue = null;
                        break;

                    case DateAnswerValue dateValue:
                        dest.DateValue = dateValue.Date;
                        dest.AnswerText = null;
                        dest.SelectedOptions = null;
                        dest.RatingValue = null;
                        dest.Latitude = null;
                        dest.Longitude = null;
                        dest.LocationAccuracy = null;
                        dest.LocationTimestamp = null;
                        dest.NumberValue = null;
                        break;

                    case null:
                        // Legacy fallback: convert from AnswerText/AnswerJson
                        // This handles old data that doesn't have Value populated
                        if (src.Question != null)
                        {
                            var legacyValue = AnswerValueFactory.ConvertFromLegacy(
                                src.AnswerText, src.AnswerJson, src.Question.QuestionType);

                            switch (legacyValue)
                            {
                                case TextAnswerValue text:
                                    dest.AnswerText = text.Text;
                                    dest.SelectedOptions = null;
                                    dest.RatingValue = null;
                                    dest.Latitude = null;
                                    dest.Longitude = null;
                                    dest.LocationAccuracy = null;
                                    dest.LocationTimestamp = null;
                                    dest.NumberValue = null;
                                    dest.DateValue = null;
                                    break;
                                case SingleChoiceAnswerValue single:
                                    dest.AnswerText = null;
                                    dest.SelectedOptions = new List<string> { single.SelectedOption };
                                    dest.RatingValue = null;
                                    dest.Latitude = null;
                                    dest.Longitude = null;
                                    dest.LocationAccuracy = null;
                                    dest.LocationTimestamp = null;
                                    dest.NumberValue = null;
                                    dest.DateValue = null;
                                    break;
                                case MultipleChoiceAnswerValue multiple:
                                    dest.AnswerText = null;
                                    dest.SelectedOptions = multiple.SelectedOptions.ToList();
                                    dest.RatingValue = null;
                                    dest.Latitude = null;
                                    dest.Longitude = null;
                                    dest.LocationAccuracy = null;
                                    dest.LocationTimestamp = null;
                                    dest.NumberValue = null;
                                    dest.DateValue = null;
                                    break;
                                case RatingAnswerValue rating:
                                    dest.AnswerText = null;
                                    dest.SelectedOptions = null;
                                    dest.RatingValue = rating.Rating;
                                    dest.Latitude = null;
                                    dest.Longitude = null;
                                    dest.LocationAccuracy = null;
                                    dest.LocationTimestamp = null;
                                    dest.NumberValue = null;
                                    dest.DateValue = null;
                                    break;
                                case LocationAnswerValue location:
                                    dest.AnswerText = null;
                                    dest.SelectedOptions = null;
                                    dest.RatingValue = null;
                                    dest.Latitude = location.Latitude;
                                    dest.Longitude = location.Longitude;
                                    dest.LocationAccuracy = location.Accuracy;
                                    dest.LocationTimestamp = location.Timestamp;
                                    dest.NumberValue = null;
                                    dest.DateValue = null;
                                    break;
                                case NumberAnswerValue number:
                                    dest.NumberValue = number.Value;
                                    dest.AnswerText = null;
                                    dest.SelectedOptions = null;
                                    dest.RatingValue = null;
                                    dest.Latitude = null;
                                    dest.Longitude = null;
                                    dest.LocationAccuracy = null;
                                    dest.LocationTimestamp = null;
                                    dest.DateValue = null;
                                    break;
                                case DateAnswerValue date:
                                    dest.DateValue = date.Date;
                                    dest.AnswerText = null;
                                    dest.SelectedOptions = null;
                                    dest.RatingValue = null;
                                    dest.Latitude = null;
                                    dest.Longitude = null;
                                    dest.LocationAccuracy = null;
                                    dest.LocationTimestamp = null;
                                    dest.NumberValue = null;
                                    break;
                                default:
                                    // No value could be extracted
                                    dest.AnswerText = src.AnswerText;
                                    break;
                            }
                        }
                        else
                        {
                            // No question context available, just use AnswerText
                            dest.AnswerText = src.AnswerText;
                        }
                        break;
                }

                // Set DisplayValue from AnswerValue if available
                dest.DisplayValue = src.Value?.DisplayValue;
            });

        // CreateAnswerDto -> Answer (DTO to Entity for creation)
        // Note: In v1.5.0, ResponseService handles answer creation via AnswerValueFactory
        // This mapping is kept for backward compatibility but most logic is in the service
        CreateMap<CreateAnswerDto, Answer>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ResponseId, opt => opt.Ignore()) // Set by service
            .ForMember(dest => dest.AnswerText, opt => opt.MapFrom(src => src.AnswerText))
            .ForMember(dest => dest.AnswerJson, opt => opt.Ignore()) // Legacy, set by service if needed
            .ForMember(dest => dest.Value, opt => opt.Ignore()) // Set by service via AnswerValueFactory
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.Response, opt => opt.Ignore())
            .ForMember(dest => dest.Question, opt => opt.Ignore())
            .ForMember(dest => dest.Next, opt => opt.Ignore()); // Set by ResponseService during flow navigation
    }
}
