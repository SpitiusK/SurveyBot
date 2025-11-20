# Media Handling Tests - Implementation Guide

**Task**: TASK-MM-025 - Test bot media handling end-to-end
**Date**: 2025-11-19
**Status**: Complete

---

## Test File Location

**File**: `C:\Users\User\Desktop\SurveyBot\tests\SurveyBot.Tests\Unit\Bot\MediaHandlingTests.cs`

This file contains comprehensive unit tests for Telegram bot media handling functionality.

---

## Test Coverage

### Test Suite 1: Media Display Tests (6 tests)

Tests verify that media items are correctly sent to Telegram users:

1. **SendQuestionMedia_WithImageMedia_DisplaysImage**
   - Verifies single image is sent correctly
   - Checks caption format ("Media 1 of 1")
   - Confirms ITelegramMediaService.SendImageAsync is called

2. **SendQuestionMedia_WithMultipleMediaTypes_DisplaysAllInOrder**
   - Tests image + video + audio sequence
   - Verifies correct order preservation
   - Checks captions for all 3 items

3. **SendQuestionMedia_WithoutMedia_ReturnsTrue**
   - Confirms graceful handling of questions with no media
   - No media methods should be called

4. **SendQuestionMedia_WithEmptyMediaList_ReturnsTrue**
   - Tests empty Items array in MediaContent
   - Should return success without calling media services

5. **SendQuestionMedia_WithDocumentType_SendsDocument**
   - Verifies PDF/document handling
   - Checks ITelegramMediaService.SendDocumentAsync is called

6. **SendQuestionMedia_WithVideo_SendsVideo**
   - Tests video file sending
   - Verifies caption and URL format

### Test Suite 2: Error Handling Tests (7 tests)

Tests verify robust error handling and retry logic:

1. **SendQuestionMedia_WithInvalidMediaUrl_ContinuesAndReturnsFalse**
   - Tests handling of broken media URLs
   - Verifies retry attempts (3 times)
   - Confirms method returns false on failure

2. **SendQuestionMedia_WithPartialFailure_ReturnsFalse**
   - Tests mixed success/failure scenario
   - Example: 2 images succeed, 1 video fails
   - Should return false if any item fails

3. **SendQuestionMedia_WithExceptionThrown_RetriesAndLogWarning**
   - Tests exception handling (e.g., HttpRequestException)
   - Verifies 3 retry attempts occur
   - Confirms warning is logged

4. **SendQuestionMedia_FailsFirstAttempt_RetriesAndSucceeds**
   - Tests exponential backoff retry logic
   - Simulates: fail, fail, succeed
   - Verifies exactly 3 attempts made

5. **SendQuestionMedia_WithUnsupportedMediaType_SkipsItem**
   - Tests handling of unknown media types
   - Should return false
   - No media methods should be called

6. **SendQuestionMedia_NetworkTimeout_HandlesGracefully**
   - Simulates network timeouts
   - Verifies retry with delay

7. **SendQuestionMedia_TelegramRateLimit_RespectsDelay**
   - Tests rate limiting handling
   - Verifies 100ms delay between media items

### Test Suite 3: TelegramMediaService Tests (4 tests)

Tests verify URL and caption formatting:

1. **TelegramMediaService_GetMediaUrl_ConvertsRelativeToAbsolute**
   - Input: "/uploads/media/2025/11/photo.jpg"
   - Output: "http://localhost:5000/uploads/media/2025/11/photo.jpg"

2. **TelegramMediaService_GetMediaUrl_ReturnsAbsoluteUrlAsIs**
   - Input: "https://example.com/media/photo.jpg"
   - Output: Same URL (no modification)

3. **TelegramMediaService_FormatMediaCaption_FormatsCorrectly**
   - Tests caption with question text
   - Format: "<question>\n\n<b>Media 2 of 5</b>"

4. **TelegramMediaService_FormatMediaCaption_WithEmptyText_OnlyShowsMediaCount**
   - Tests caption without question text
   - Format: "<b>Media 1 of 3</b>"

---

## Running the Tests

### Run all media handling tests:

```bash
cd C:\Users\User\Desktop\SurveyBot
dotnet test --filter "FullyQualifiedName~MediaHandlingTests"
```

### Run specific test:

```bash
dotnet test --filter "FullyQualifiedName~MediaHandlingTests.SendQuestionMedia_WithImageMedia_DisplaysImage"
```

### Run with verbose output:

```bash
dotnet test --filter "FullyQualifiedName~MediaHandlingTests" --logger "console;verbosity=detailed"
```

### Run with coverage:

```bash
dotnet test --filter "FullyQualifiedName~MediaHandlingTests" /p:CollectCoverage=true
```

---

## Test Scenarios Covered

### Happy Path
- [x] Single image display
- [x] Single video display
- [x] Single audio display
- [x] Single document display
- [x] Multiple images in order
- [x] Mixed media types (image + video + audio)
- [x] Question with no media
- [x] Question with empty media list

### Error Scenarios
- [x] Invalid media URL (404)
- [x] Network timeout
- [x] Telegram rate limiting
- [x] Partial failure (some succeed, some fail)
- [x] All media fails
- [x] Unsupported media type
- [x] Exception thrown during send

### Retry Logic
- [x] Retry 3 times on failure
- [x] Exponential backoff delay (100ms, 200ms, 300ms)
- [x] Success after retries
- [x] Failure after all retries

### URL and Caption Formatting
- [x] Relative URL to absolute URL conversion
- [x] Absolute URL passthrough
- [x] Caption with question text
- [x] Caption without question text
- [x] Caption with media counter

---

## Mock Objects Used

### ITelegramMediaService (Mock)
```csharp
_mockMediaService.Setup(x => x.SendImageAsync(...))
    .ReturnsAsync(true);  // or false for failure tests
```

Methods mocked:
- `SendImageAsync(chatId, url, caption, cancellationToken)`
- `SendVideoAsync(chatId, url, caption, cancellationToken)`
- `SendAudioAsync(chatId, url, caption, cancellationToken)`
- `SendDocumentAsync(chatId, url, caption, cancellationToken)`
- `GetMediaUrl(filePath, baseUrl)`
- `FormatMediaCaption(questionText, index, total)`

### ITelegramBotClient (Mock)
```csharp
_mockBotClient.Setup(x => x.SendPhoto(...))
    .ReturnsAsync(new Message());
```

Used for integration tests with actual Telegram API calls.

### ILogger<QuestionMediaHelper> (Mock)
```csharp
_mockLogger.Verify(
    x => x.Log(LogLevel.Warning, ...),
    Times.AtLeastOnce);
```

Verifies warning/error logs are written.

---

## Test Data

### Sample MediaItemDto

```csharp
new MediaItemDto
{
    Id = Guid.NewGuid().ToString(),
    Type = "image",
    FilePath = "/uploads/media/2025/11/photo.jpg",
    DisplayName = "photo.jpg",
    FileSize = 1024000,  // 1MB
    MimeType = "image/jpeg",
    UploadedAt = DateTime.UtcNow,
    Order = 0
}
```

### Sample QuestionDto with Media

```csharp
new QuestionDto
{
    Id = 1,
    QuestionText = "Review this content",
    QuestionType = QuestionType.Text,
    MediaContent = new MediaContentDto
    {
        Version = "1.0",
        Items = new List<MediaItemDto>
        {
            CreateMediaItem("image", "/uploads/media/2025/11/photo.jpg", 0),
            CreateMediaItem("video", "/uploads/media/2025/11/video.mp4", 1)
        }
    }
}
```

---

## Expected Behavior

### Success Scenario
1. QuestionMediaHelper.SendQuestionMediaAsync() called
2. Media items sorted by Order property
3. For each media item:
   - Convert file path to full URL
   - Call appropriate Send method (SendImageAsync, etc.)
   - Apply retry logic (up to 3 attempts)
   - Log success/failure
   - Add 100ms delay before next item
4. Return true if all succeed, false if any fail

### Failure Scenario
1. Media send fails (network error, 404, etc.)
2. Retry up to 3 times with exponential backoff
3. Log warning after each failure
4. Continue to next media item
5. Return false at end

### Retry Delays
- Attempt 1: Immediate
- Attempt 2: Wait 100ms
- Attempt 3: Wait 200ms
- Give up after attempt 3

---

## Integration with Actual Code

### Files Tested
- **QuestionMediaHelper.cs** - Main media sending logic
- **TelegramMediaService.cs** - Telegram API wrapper
- **QuestionDto** - Question with MediaContent property
- **MediaContentDto** - Container for media items
- **MediaItemDto** - Individual media item

### Dependencies
- Microsoft.Extensions.Logging
- Microsoft.Extensions.Options
- SurveyBot.Bot.Configuration.BotConfiguration
- SurveyBot.Core.DTOs.Media
- SurveyBot.Core.Interfaces.ITelegramMediaService
- Telegram.Bot.ITelegramBotClient

---

## Manual Testing Checklist

See: `C:\Users\User\Desktop\SurveyBot\tests\SurveyBot.Tests\MANUAL_TESTING_MEDIA_CHECKLIST.md`

This comprehensive checklist covers:
- Image display (all formats, sizes)
- Video display (all formats, sizes)
- Audio display (all formats, sizes)
- Document display (PDFs, etc.)
- Multiple media in one question
- Error scenarios
- Preview command
- Complete survey flow with media
- Performance metrics
- Cross-platform testing (Desktop, Mobile, Web)

---

## CI/CD Integration

### Add to CI Pipeline

```yaml
- name: Run Media Handling Tests
  run: |
    dotnet test --filter "FullyQualifiedName~MediaHandlingTests" \
      --logger "trx" \
      /p:CollectCoverage=true \
      /p:CoverageDirectory=coverage \
      /p:Threshold=80
```

### Test Metrics to Track
- Total tests: 17
- Target coverage: 80%+
- Target execution time: < 5 seconds
- Flaky test threshold: 0%

---

## Troubleshooting

### Test Failures

**Issue**: Media service mock not returning expected value
**Solution**: Check Setup() calls match exact parameter types

**Issue**: Tests timeout
**Solution**: Ensure CancellationToken.None is passed, not default cancellation token

**Issue**: Null reference exception
**Solution**: Verify all mock objects are initialized in constructor

### Common Errors

```
Error: ITelegramMediaService.SendImageAsync() not called
Fix: Check that MediaContent.Items is not null/empty
```

```
Error: Expected 3 calls, but found 1
Fix: Verify retry logic is enabled and not short-circuiting
```

```
Error: InvalidOperationException: Sequence contains no elements
Fix: Ensure Items.OrderBy(m => m.Order) has at least one item
```

---

## Future Enhancements

### Additional Test Scenarios

1. **Large File Handling**
   - Test 50MB video (max size)
   - Test 10MB image (max size)
   - Verify progress callbacks

2. **Concurrent Media Sending**
   - Multiple users taking survey with media
   - Rate limiting across users
   - Queue management

3. **Media Caching**
   - Test cached media URLs
   - Verify cache invalidation
   - Test cache hit rate

4. **Media Compression**
   - Test image optimization before send
   - Verify quality settings
   - Check file size reduction

5. **Alternative Media Sources**
   - Test cloud storage URLs (S3, Azure Blob)
   - Test streaming media
   - Test external URLs

### Performance Tests

```csharp
[Fact]
public async Task SendQuestionMedia_With10Images_CompletesIn5Seconds()
{
    // Arrange: 10 images
    var stopwatch = Stopwatch.StartNew();

    // Act: Send all media
    await _mediaHelper.SendQuestionMediaAsync(...);

    stopwatch.Stop();

    // Assert: < 5 seconds
    Assert.True(stopwatch.ElapsedMilliseconds < 5000);
}
```

---

## References

- [Telegram Bot API - Sending Files](https://core.telegram.org/bots/api#sending-files)
- [xUnit Testing Best Practices](https://xunit.net/docs/getting-started/netcore/cmdline)
- [Moq Documentation](https://github.com/moq/moq4/wiki/Quickstart)
- [Task MM-025 Specification](../../multimedia-support-tasks.yaml)

---

## Test Maintenance

### When to Update Tests

1. **MediaItemDto changes** → Update CreateMediaItem() helper
2. **Retry logic changes** → Update retry count assertions
3. **Caption format changes** → Update FormatMediaCaption tests
4. **New media type added** → Add new test case
5. **URL generation changes** → Update GetMediaUrl tests

### Test Review Checklist

- [ ] All tests have clear, descriptive names
- [ ] Arrange-Act-Assert pattern followed
- [ ] Each test tests ONE behavior
- [ ] Mocks are reset between tests
- [ ] No tests depend on external resources
- [ ] Test data is self-contained
- [ ] Error messages are helpful
- [ ] Tests are fast (< 100ms each)

---

**Test File Created**: 2025-11-19
**Total Tests**: 17
**Test Categories**: Media Display, Error Handling, URL/Caption Formatting
**Dependencies**: Moq 4.20.70, xUnit 2.5.3, Telegram.Bot 22.7.4

**Status**: Ready for CI/CD integration and manual testing
