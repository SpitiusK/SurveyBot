# ResponseService Answer Validation Examples

This document provides examples of answer validation for different question types in the ResponseService.

## Overview

The ResponseService validates answers based on the question type before saving them to the database. This ensures data integrity and provides immediate feedback to users.

## Validation Methods

### ValidateAnswerFormatAsync

The main validation method that routes to specific validators based on question type:

```csharp
public async Task<ValidationResult> ValidateAnswerFormatAsync(
    int questionId,
    string? answerText = null,
    List<string>? selectedOptions = null,
    int? ratingValue = null)
```

## Question Type Validations

### 1. Text Questions

**Rules:**
- Required questions must have non-empty text
- Text cannot exceed 5000 characters
- Optional questions can have null/empty text

**Examples:**

```csharp
// Valid text answer
var result = await responseService.ValidateAnswerFormatAsync(
    questionId: 1,
    answerText: "This is my answer"
);
// result.IsValid = true

// Invalid: Empty required answer
var result = await responseService.ValidateAnswerFormatAsync(
    questionId: 1,
    answerText: ""
);
// result.IsValid = false
// result.ErrorMessage = "Text answer is required"

// Invalid: Text too long
var longText = new string('a', 5001);
var result = await responseService.ValidateAnswerFormatAsync(
    questionId: 1,
    answerText: longText
);
// result.IsValid = false
// result.ErrorMessage = "Text answer cannot exceed 5000 characters"
```

### 2. Single Choice Questions

**Rules:**
- Exactly one option must be selected
- Selected option must exist in question's options list
- Required questions must have a selection

**Question Setup:**
```json
{
  "questionType": "SingleChoice",
  "options": ["Option A", "Option B", "Option C"]
}
```

**Examples:**

```csharp
// Valid single choice answer
var result = await responseService.ValidateAnswerFormatAsync(
    questionId: 2,
    selectedOptions: new List<string> { "Option A" }
);
// result.IsValid = true

// Invalid: Multiple options selected
var result = await responseService.ValidateAnswerFormatAsync(
    questionId: 2,
    selectedOptions: new List<string> { "Option A", "Option B" }
);
// result.IsValid = false
// result.ErrorMessage = "Only one option can be selected for single choice questions"

// Invalid: Option not in list
var result = await responseService.ValidateAnswerFormatAsync(
    questionId: 2,
    selectedOptions: new List<string> { "Invalid Option" }
);
// result.IsValid = false
// result.ErrorMessage = "Selected option is not valid for this question"

// Invalid: Required with no selection
var result = await responseService.ValidateAnswerFormatAsync(
    questionId: 2,
    selectedOptions: null
);
// result.IsValid = false
// result.ErrorMessage = "An option must be selected"
```

### 3. Multiple Choice Questions

**Rules:**
- One or more options can be selected
- All selected options must exist in question's options list
- Required questions must have at least one selection

**Question Setup:**
```json
{
  "questionType": "MultipleChoice",
  "options": ["Option A", "Option B", "Option C", "Option D"]
}
```

**Examples:**

```csharp
// Valid: Single option
var result = await responseService.ValidateAnswerFormatAsync(
    questionId: 3,
    selectedOptions: new List<string> { "Option A" }
);
// result.IsValid = true

// Valid: Multiple options
var result = await responseService.ValidateAnswerFormatAsync(
    questionId: 3,
    selectedOptions: new List<string> { "Option A", "Option C", "Option D" }
);
// result.IsValid = true

// Invalid: Contains invalid option
var result = await responseService.ValidateAnswerFormatAsync(
    questionId: 3,
    selectedOptions: new List<string> { "Option A", "Invalid Option" }
);
// result.IsValid = false
// result.ErrorMessage = "Invalid options selected: Invalid Option"

// Invalid: Required with no selection
var result = await responseService.ValidateAnswerFormatAsync(
    questionId: 3,
    selectedOptions: new List<string>()
);
// result.IsValid = false
// result.ErrorMessage = "At least one option must be selected"
```

### 4. Rating Questions

**Rules:**
- Rating must be an integer between 1 and 5 (inclusive)
- Required questions must have a rating value

**Examples:**

```csharp
// Valid ratings
foreach (var rating in new[] { 1, 2, 3, 4, 5 })
{
    var result = await responseService.ValidateAnswerFormatAsync(
        questionId: 4,
        ratingValue: rating
    );
    // result.IsValid = true
}

// Invalid: Rating too low
var result = await responseService.ValidateAnswerFormatAsync(
    questionId: 4,
    ratingValue: 0
);
// result.IsValid = false
// result.ErrorMessage = "Rating must be between 1 and 5"

// Invalid: Rating too high
var result = await responseService.ValidateAnswerFormatAsync(
    questionId: 4,
    ratingValue: 6
);
// result.IsValid = false
// result.ErrorMessage = "Rating must be between 1 and 5"

// Invalid: Required with no value
var result = await responseService.ValidateAnswerFormatAsync(
    questionId: 4,
    ratingValue: null
);
// result.IsValid = false
// result.ErrorMessage = "Rating is required"
```

## Complete SaveAnswer Workflow

### Example 1: Saving a Text Answer

```csharp
// 1. Start a response
var response = await responseService.StartResponseAsync(
    surveyId: 1,
    telegramUserId: 123456789,
    username: "john_doe",
    firstName: "John"
);

// 2. Save text answer
var updatedResponse = await responseService.SaveAnswerAsync(
    responseId: response.Id,
    questionId: 1,
    answerText: "I really enjoyed the service!"
);

// 3. Complete the response
var completedResponse = await responseService.CompleteResponseAsync(
    responseId: response.Id
);
```

### Example 2: Saving a Single Choice Answer

```csharp
// Save single choice answer
var updatedResponse = await responseService.SaveAnswerAsync(
    responseId: response.Id,
    questionId: 2,
    selectedOptions: new List<string> { "Very Satisfied" }
);
```

### Example 3: Saving a Multiple Choice Answer

```csharp
// Save multiple choice answer
var updatedResponse = await responseService.SaveAnswerAsync(
    responseId: response.Id,
    questionId: 3,
    selectedOptions: new List<string>
    {
        "Fast Delivery",
        "Good Quality",
        "Affordable Price"
    }
);
```

### Example 4: Saving a Rating Answer

```csharp
// Save rating answer
var updatedResponse = await responseService.SaveAnswerAsync(
    responseId: response.Id,
    questionId: 4,
    ratingValue: 5
);
```

## Error Handling

The service throws specific exceptions for different error scenarios:

```csharp
try
{
    var result = await responseService.SaveAnswerAsync(
        responseId: 1,
        questionId: 2,
        answerText: "Invalid answer type"
    );
}
catch (ResponseNotFoundException ex)
{
    // Response with ID 1 not found
    Console.WriteLine($"Response not found: {ex.ResponseId}");
}
catch (QuestionNotFoundException ex)
{
    // Question with ID 2 not found
    Console.WriteLine($"Question not found: {ex.QuestionId}");
}
catch (InvalidAnswerFormatException ex)
{
    // Answer format doesn't match question type
    Console.WriteLine($"Invalid answer format for question {ex.QuestionId} ({ex.QuestionType}): {ex.Reason}");
}
catch (SurveyOperationException ex)
{
    // Response already completed or other operation error
    Console.WriteLine($"Operation error: {ex.Message}");
}
```

## Response Flow

### 1. Start New Response
```csharp
var response = await responseService.StartResponseAsync(
    surveyId: 1,
    telegramUserId: 123456789
);
// Response created with IsComplete = false, StartedAt = Now
```

### 2. Resume Incomplete Response
```csharp
var response = await responseService.ResumeResponseAsync(
    surveyId: 1,
    telegramUserId: 123456789
);
// Returns existing incomplete response or starts new one
```

### 3. Save Multiple Answers
```csharp
// User can save answers one by one
await responseService.SaveAnswerAsync(responseId, question1Id, answerText: "Answer 1");
await responseService.SaveAnswerAsync(responseId, question2Id, selectedOptions: new[] { "Option A" });
await responseService.SaveAnswerAsync(responseId, question3Id, ratingValue: 4);
```

### 4. Update Existing Answer
```csharp
// Calling SaveAnswerAsync again with same questionId updates the answer
await responseService.SaveAnswerAsync(
    responseId,
    question1Id,
    answerText: "Updated Answer"
);
```

### 5. Complete Response
```csharp
var completedResponse = await responseService.CompleteResponseAsync(responseId);
// Response marked as complete with SubmittedAt = Now
// No more answers can be added after completion
```

## Validation Result Structure

```csharp
public class ValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, string>? Details { get; set; }

    public static ValidationResult Success();
    public static ValidationResult Failure(string errorMessage);
    public static ValidationResult Failure(string errorMessage, Dictionary<string, string> details);
}
```

## Integration with Telegram Bot

The bot can use these methods to validate user input before saving:

```csharp
// In bot handler
var validationResult = await responseService.ValidateAnswerFormatAsync(
    questionId: currentQuestion.Id,
    answerText: userMessage
);

if (!validationResult.IsValid)
{
    await botClient.SendTextMessageAsync(
        chatId,
        $"Invalid answer: {validationResult.ErrorMessage}"
    );
    return;
}

// Save the answer
await responseService.SaveAnswerAsync(
    responseId: currentResponse.Id,
    questionId: currentQuestion.Id,
    answerText: userMessage
);
```
