# DTO Validation Summary

## Overview

This document provides a comprehensive overview of all validation rules applied to DTOs in the SurveyBot application.

## Validation Attributes Used

| Attribute | Purpose | Example |
|-----------|---------|---------|
| `[Required]` | Field must have a value | `[Required(ErrorMessage = "Title is required")]` |
| `[MaxLength(n)]` | String max length | `[MaxLength(500)]` |
| `[MinLength(n)]` | String min length | `[MinLength(3)]` |
| `[Range(min, max)]` | Numeric range | `[Range(1, 5)]` |
| `[EnumDataType(typeof(T))]` | Enum validation | `[EnumDataType(typeof(QuestionType))]` |

## Survey DTOs Validation

### CreateSurveyDto
| Field | Required | Type | Constraints |
|-------|----------|------|-------------|
| Title | Yes | string | 3-500 characters |
| Description | No | string? | Max 2000 characters |
| IsActive | No | bool | Default: false |
| AllowMultipleResponses | No | bool | Default: false |
| ShowResults | No | bool | Default: true |

### UpdateSurveyDto
| Field | Required | Type | Constraints |
|-------|----------|------|-------------|
| Title | Yes | string | 3-500 characters |
| Description | No | string? | Max 2000 characters |
| AllowMultipleResponses | No | bool | - |
| ShowResults | No | bool | - |

### ToggleSurveyStatusDto
| Field | Required | Type | Constraints |
|-------|----------|------|-------------|
| IsActive | Yes | bool | - |

## Question DTOs Validation

### CreateQuestionDto
| Field | Required | Type | Constraints |
|-------|----------|------|-------------|
| QuestionText | Yes | string | 3-1000 characters |
| QuestionType | Yes | QuestionType | Valid enum value |
| IsRequired | No | bool | Default: true |
| Options | Conditional | List<string>? | See below |

**Options Validation Rules:**
- **SingleChoice/MultipleChoice**:
  - Required: Must have at least 2 options
  - Maximum: 10 options
  - Each option: Max 200 characters, non-empty
- **Text/Rating**:
  - Should be null or empty
  - Error if options provided

### UpdateQuestionDto
Same validation as CreateQuestionDto.

### ReorderQuestionsDto
| Field | Required | Type | Constraints |
|-------|----------|------|-------------|
| QuestionIds | Yes | List<int> | At least 1 item |

## Response DTOs Validation

### CreateResponseDto
| Field | Required | Type | Constraints |
|-------|----------|------|-------------|
| RespondentTelegramId | Yes | long | Positive value (>= 1) |
| RespondentUsername | No | string? | Max 255 characters |
| RespondentFirstName | No | string? | Max 255 characters |

### CompleteResponseDto
| Field | Required | Type | Constraints |
|-------|----------|------|-------------|
| RespondentTelegramId | Yes | long | Positive value (>= 1) |
| RespondentUsername | No | string? | Max 255 characters |
| RespondentFirstName | No | string? | Max 255 characters |
| Answers | Yes | List<CreateAnswerDto> | At least 1 item |

### SubmitAnswerDto
| Field | Required | Type | Constraints |
|-------|----------|------|-------------|
| Answer | Yes | CreateAnswerDto | Valid CreateAnswerDto |

## Answer DTOs Validation

### CreateAnswerDto
| Field | Required | Type | Constraints |
|-------|----------|------|-------------|
| QuestionId | Yes | int | Positive value (>= 1) |
| AnswerText | Conditional | string? | Max 5000 characters |
| SelectedOptions | Conditional | List<string>? | Required for choice questions |
| RatingValue | Conditional | int? | 1-5 range |

**Field Requirements by Question Type:**
- **Text**: AnswerText required (max 5000 chars)
- **SingleChoice**: SelectedOptions required (1 item)
- **MultipleChoice**: SelectedOptions required (1+ items)
- **Rating**: RatingValue required (1-5)

## User & Authentication DTOs Validation

### LoginDto
| Field | Required | Type | Constraints |
|-------|----------|------|-------------|
| InitData | Yes | string | Non-empty |
| TelegramId | No | long? | Positive if provided |
| Username | No | string? | Max 255 characters |

**Note:** Either InitData OR (TelegramId + Username) should be provided for authentication.

### RegisterDto
| Field | Required | Type | Constraints |
|-------|----------|------|-------------|
| TelegramId | Yes | long | Positive value (>= 1) |
| Username | No | string? | Max 255 characters |
| FirstName | No | string? | Max 255 characters |
| LastName | No | string? | Max 255 characters |

### RefreshTokenDto
| Field | Required | Type | Constraints |
|-------|----------|------|-------------|
| RefreshToken | Yes | string | Non-empty |

## Common DTOs Validation

### PaginationQueryDto
| Field | Required | Type | Constraints |
|-------|----------|------|-------------|
| PageNumber | No | int | Min: 1, Default: 1 |
| PageSize | No | int | 1-100, Default: 10 |
| SearchTerm | No | string? | Max 200 characters |
| SortBy | No | string? | Max 50 characters |
| SortDescending | No | bool | Default: false |

## Custom Validation Logic

### CreateQuestionDto.Validate()
```csharp
public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
{
    if (QuestionType == QuestionType.SingleChoice ||
        QuestionType == QuestionType.MultipleChoice)
    {
        if (Options == null || Options.Count < 2)
            yield return new ValidationResult(
                "Choice-based questions must have at least 2 options",
                new[] { nameof(Options) });

        if (Options?.Count > 10)
            yield return new ValidationResult(
                "Questions cannot have more than 10 options",
                new[] { nameof(Options) });

        if (Options?.Any(string.IsNullOrWhiteSpace) == true)
            yield return new ValidationResult(
                "All options must have text",
                new[] { nameof(Options) });

        if (Options?.Any(o => o.Length > 200) == true)
            yield return new ValidationResult(
                "Option text cannot exceed 200 characters",
                new[] { nameof(Options) });
    }
    else if (Options != null && Options.Any())
    {
        yield return new ValidationResult(
            "Text and Rating questions should not have options",
            new[] { nameof(Options) });
    }
}
```

## Validation Error Response Format

### Single Field Error
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Title": [
      "Title is required"
    ]
  }
}
```

### Multiple Field Errors
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Title": [
      "Title is required",
      "Title must be at least 3 characters"
    ],
    "Options": [
      "Choice-based questions must have at least 2 options"
    ]
  }
}
```

## Business Validation (Service Layer)

Beyond DTO validation, the service layer should enforce:

### Survey Business Rules
- Cannot activate survey without questions
- Cannot delete survey with responses (or require confirmation)
- Cannot edit active survey structure (questions)
- Creator must exist in database

### Question Business Rules
- Survey must exist
- OrderIndex must be unique within survey
- Cannot delete question with answers (or cascade)
- Cannot change question type if answers exist

### Response Business Rules
- Survey must be active to accept responses
- Cannot submit multiple responses if not allowed
- Must answer all required questions before completing
- Answer must match question type

### Answer Business Rules
- Response must exist and be incomplete
- Question must belong to the survey
- Rating value must be 1-5
- Selected options must be from question's options list

## Validation Testing Strategy

### Unit Tests
Test each DTO validation independently:
```csharp
[Theory]
[InlineData("")]
[InlineData("ab")]  // Too short
[InlineData(/* 501 char string */)]  // Too long
public void CreateSurveyDto_InvalidTitle_FailsValidation(string title)
{
    var dto = new CreateSurveyDto { Title = title };
    var context = new ValidationContext(dto);
    var results = new List<ValidationResult>();

    var isValid = Validator.TryValidateObject(dto, context, results, true);

    Assert.False(isValid);
    Assert.Contains(results, r => r.MemberNames.Contains(nameof(dto.Title)));
}
```

### Integration Tests
Test validation in API context:
```csharp
[Fact]
public async Task CreateSurvey_InvalidDto_Returns400()
{
    var dto = new CreateSurveyDto { Title = "" }; // Invalid

    var response = await _client.PostAsJsonAsync("/api/surveys", dto);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
}
```

## Common Validation Patterns

### Required Field Pattern
```csharp
[Required(ErrorMessage = "{0} is required")]
[MaxLength(500, ErrorMessage = "{0} cannot exceed {1} characters")]
public string Title { get; set; } = string.Empty;
```

### Optional Field Pattern
```csharp
[MaxLength(2000, ErrorMessage = "{0} cannot exceed {1} characters")]
public string? Description { get; set; }
```

### Numeric Range Pattern
```csharp
[Required]
[Range(1, int.MaxValue, ErrorMessage = "{0} must be positive")]
public int QuestionId { get; set; }
```

### Enum Validation Pattern
```csharp
[Required(ErrorMessage = "{0} is required")]
[EnumDataType(typeof(QuestionType), ErrorMessage = "Invalid {0}")]
public QuestionType QuestionType { get; set; }
```

## Validation Best Practices

1. **Error Messages**: Always provide clear, user-friendly error messages
2. **Field Names**: Use `{0}` placeholder for field name in messages
3. **Constraints**: Use `{1}`, `{2}` for constraint values (min, max)
4. **Consistency**: Apply same validation rules across Create/Update DTOs
5. **Business Logic**: Keep business validation in service layer, not DTOs
6. **Testing**: Write tests for both valid and invalid scenarios
7. **Documentation**: Document custom validation logic clearly
8. **Localization**: Consider using resource files for error messages

## Performance Considerations

- Validation occurs before service layer processing
- Failed validation returns 400 immediately (fast failure)
- Complex validation in `IValidatableObject` runs after attribute validation
- Consider FluentValidation for very complex scenarios

## Security Considerations

- Validation helps prevent over-posting attacks
- Always validate on server side (never trust client validation)
- Use `[MaxLength]` to prevent excessive data
- Validate enum values to prevent invalid data injection
- Sanitize text inputs in service layer if needed
