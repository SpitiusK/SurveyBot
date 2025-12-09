# DTO Validation Unit Tests

This directory contains unit tests for Data Transfer Object (DTO) validation in SurveyBot.

## Test Files

### CreateQuestionWithFlowDtoTests.cs

**Purpose**: Regression tests for v1.6.2 Rating question conditional flow validation bug fix.

**Test Count**: 20 tests

**Coverage**:
- Rating question conditional flow validation (4 tests)
- SingleChoice question conditional flow validation (1 test)
- Invalid question type validation (5 tests)
- SupportsBranching property tests (7 tests)
- Edge case validation (3 tests)

**Key Tests**:
- `Validate_RatingWithOptionNextQuestionIndexes_ShouldBeValid` - Main bug reproduction test
- `Validate_SingleChoiceWithOptionNextQuestionIndexes_ShouldBeValid` - Regression test for SingleChoice
- `Validate_TextWithOptionNextQuestionIndexes_ShouldBeInvalid` - Negative test for non-branching types

**Run Tests**:
```bash
dotnet test --filter "FullyQualifiedName~CreateQuestionWithFlowDtoTests"
```

**Expected Result**: All 20 tests pass

## Bug Fixed

**Bug**: Survey publishing failed with 400 Bad Request when Rating questions had conditional flow configured.

**Fix**: Updated `CreateQuestionWithFlowDto.cs` to allow both SingleChoice AND Rating questions to use `optionNextQuestionIndexes`.

**Files Changed**:
- `src/SurveyBot.Core/DTOs/Question/CreateQuestionWithFlowDto.cs` (line 136)

**Version**: v1.6.2

## Test Maintenance

When modifying `CreateQuestionWithFlowDto` validation logic:

1. **Check existing tests** - Ensure all tests still pass
2. **Add new tests** - For any new validation rules
3. **Update documentation** - Reflect validation changes
4. **Run full test suite** - Verify no regressions

## Related Tests

- **Integration Tests**: `tests/SurveyBot.Tests/Integration/Services/RatingConditionalFlowIntegrationTests.cs`
- **DTO Mapping Tests**: `tests/SurveyBot.Tests/Unit/Mapping/*`

## Documentation

See `REGRESSION_TESTS_v1.6.2_SUMMARY.md` in project root for complete test documentation.
