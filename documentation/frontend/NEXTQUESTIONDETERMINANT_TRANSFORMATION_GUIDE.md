# NextQuestionDeterminant Transformation Guide

**Version**: 1.0
**Date**: 2025-11-23
**Audience**: Frontend Developers

---

## Quick Reference

### Value Object Structure

```typescript
// Type definition
export type NextStepType = 'GoToQuestion' | 'EndSurvey';

export interface NextQuestionDeterminant {
  type: NextStepType;
  questionId?: number | null;
}
```

### Examples

```typescript
// End survey
{ type: 'EndSurvey' }

// Go to specific question
{ type: 'GoToQuestion', questionId: 5 }

// No flow configured / sequential
null
```

---

## Transformation Patterns

### Pattern 1: API Response → Local UI State

**Use Case**: Loading flow configuration from API for display in dropdowns

**Example**: FlowConfigurationPanel.loadFlowConfig()

```typescript
// Input: NextQuestionDeterminant from API
const determinant: NextQuestionDeterminant | null = {
  type: 'GoToQuestion',
  questionId: 5
};

// Output: number for dropdown value
const dropdownValue = !determinant ? null :
  determinant.type === 'EndSurvey' ? -1 :
  determinant.questionId ?? null;

// Result: 5
```

**Convention**:
- `null` → `null` (no selection / sequential)
- `{ type: 'EndSurvey' }` → `-1`
- `{ type: 'GoToQuestion', questionId: N }` → `N`

---

### Pattern 2: Local UI State → API Request

**Use Case**: Saving dropdown selection to API

**Example**: FlowConfigurationPanel.handleSave()

```typescript
// Input: number from dropdown
const dropdownValue: number | null = 5;

// Output: NextQuestionDeterminant for API
const determinant = dropdownValue === null ? null :
  dropdownValue === -1
    ? { type: 'EndSurvey' as const }
    : { type: 'GoToQuestion' as const, questionId: dropdownValue };

// Result: { type: 'GoToQuestion', questionId: 5 }
```

**Convention**:
- `null` → `null`
- `-1` → `{ type: 'EndSurvey' }`
- `N` (number > 0) → `{ type: 'GoToQuestion', questionId: N }`

---

### Pattern 3: Draft UUID → Database ID → Determinant

**Use Case**: Publishing survey with draft questions

**Example**: ReviewStep.handlePublish()

```typescript
// PASS 1: Create questions and build mapping
const questionIdMap = new Map<string, number>();
questionIdMap.set('uuid-abc-123', 10); // Draft UUID → DB ID

// PASS 2: Transform flow references
const draftNextId = 'uuid-abc-123'; // UUID reference in draft
const dbId = questionIdMap.get(draftNextId); // → 10

const determinant = dbId === 0
  ? { type: 'EndSurvey' as const }
  : { type: 'GoToQuestion' as const, questionId: dbId };

// Result: { type: 'GoToQuestion', questionId: 10 }
```

**Special Cases**:
- Draft uses `'0'` or `null` for "End Survey"
- Converted to database ID `0`
- Then transformed to `{ type: 'EndSurvey' }`

---

## Component Usage Examples

### ReviewStep: Publishing Survey

```typescript
// Transform default next question
const defaultNext = defaultNextQuestionId === undefined ? null : (
  defaultNextQuestionId === 0
    ? { type: 'EndSurvey' as const }
    : { type: 'GoToQuestion' as const, questionId: defaultNextQuestionId }
);

// Transform option-specific flows
const optionNextQuestions = optionFlows ? Object.fromEntries(
  Object.entries(optionFlows).map(([optionId, nextId]) => [
    optionId,
    nextId === 0
      ? { type: 'EndSurvey' as const }
      : { type: 'GoToQuestion' as const, questionId: nextId }
  ])
) : undefined;

// Send to API
const payload: UpdateQuestionFlowDto = {
  defaultNext,
  optionNextQuestions,
};

await questionFlowService.updateQuestionFlow(surveyId, questionId, payload);
```

---

### FlowConfigurationPanel: Loading Flow

```typescript
// Load from API
const config = await questionFlowService.getQuestionFlow(surveyId, questionId);

// Transform defaultNext for dropdown
const defaultNext = config.defaultNext;
setDefaultNextQuestionId(
  !defaultNext ? null :
  defaultNext.type === 'EndSurvey' ? -1 :
  defaultNext.questionId ?? null
);

// Transform option flows for dropdowns
const flows: Record<number, number | null> = {};
config.optionFlows.forEach((optionFlow) => {
  const next = optionFlow.next;
  flows[optionFlow.optionId] = !next ? null :
    next.type === 'EndSurvey' ? -1 :
    next.questionId ?? null;
});
setOptionFlows(flows);
```

---

### FlowConfigurationPanel: Saving Flow

```typescript
// Transform dropdown values to determinants
const dto: UpdateQuestionFlowDto = {};

if (isBranchingQuestion) {
  // Transform option flows
  const filteredFlows: Record<number, NextQuestionDeterminant> = {};
  Object.entries(optionFlows).forEach(([key, value]) => {
    if (value !== null) {
      filteredFlows[parseInt(key)] = value === -1
        ? { type: 'EndSurvey' }
        : { type: 'GoToQuestion', questionId: value };
    }
  });
  dto.optionNextQuestions = filteredFlows;
} else {
  // Transform default next
  dto.defaultNext = defaultNextQuestionId === null ? null :
    defaultNextQuestionId === -1
      ? { type: 'EndSurvey' }
      : { type: 'GoToQuestion', questionId: defaultNextQuestionId };
}

// Send to API
await questionFlowService.updateQuestionFlow(surveyId, questionId, dto);
```

---

### FlowVisualization: Displaying Flow

```typescript
// Load flow config
const flowConfig = await questionFlowService.getQuestionFlow(surveyId, questionId);

// Extract next question for visualization
if (flowConfig.supportsBranching && flowConfig.optionFlows.length > 0) {
  flowConfig.optionFlows.forEach((optionFlow) => {
    const next = optionFlow.next;
    const visualValue = !next ? null :
      next.type === 'EndSurvey' ? -1 :
      next.questionId ?? null;

    // Use visualValue for rendering nodes/edges
  });
} else if (flowConfig.defaultNext) {
  const next = flowConfig.defaultNext;
  const visualValue = next.type === 'EndSurvey' ? -1 : next.questionId ?? null;

  // Use visualValue for rendering
}
```

---

## Common Pitfalls

### ❌ Incorrect: Using magic numbers directly

```typescript
// BAD: Comparing with magic number
if (config.defaultNext === 0) { ... }

// BAD: Sending number directly to API
const payload = { defaultNext: 0 };
```

### ✅ Correct: Using type-safe value objects

```typescript
// GOOD: Check type property
if (config.defaultNext?.type === 'EndSurvey') { ... }

// GOOD: Send value object to API
const payload = {
  defaultNext: { type: 'EndSurvey' }
};
```

---

### ❌ Incorrect: Forgetting null handling

```typescript
// BAD: Crashes if null
const dropdownValue = determinant.questionId;

// BAD: Wrong fallback
const dropdownValue = determinant?.questionId || -1;
```

### ✅ Correct: Proper null handling

```typescript
// GOOD: Explicit null check with ternary
const dropdownValue = !determinant ? null :
  determinant.type === 'EndSurvey' ? -1 :
  determinant.questionId ?? null;
```

---

### ❌ Incorrect: Missing 'as const' assertion

```typescript
// BAD: Type not narrowed to literal
{ type: 'EndSurvey' } // type is string

// BAD: Can cause type errors
const determinant = value === -1
  ? { type: 'EndSurvey' }
  : { type: 'GoToQuestion', questionId: value };
```

### ✅ Correct: Use 'as const' for type literals

```typescript
// GOOD: Type narrowed to 'EndSurvey' literal
{ type: 'EndSurvey' as const }

// GOOD: TypeScript knows exact types
const determinant = value === -1
  ? { type: 'EndSurvey' as const }
  : { type: 'GoToQuestion' as const, questionId: value };
```

---

## Type Definitions Reference

### Core Types

```typescript
// Located in: frontend/src/types/index.ts

export type NextStepType = 'GoToQuestion' | 'EndSurvey';

export interface NextQuestionDeterminant {
  type: NextStepType;
  questionId?: number | null;
}
```

---

### DTOs

```typescript
// Update DTO (sent to API)
export interface UpdateQuestionFlowDto {
  defaultNext?: NextQuestionDeterminant | null;
  optionNextQuestions?: Record<number, NextQuestionDeterminant>;
}

// Flow response DTO (received from API)
export interface ConditionalFlowDto {
  questionId: number;
  supportsBranching: boolean;
  defaultNext?: NextQuestionDeterminant | null;
  optionFlows: OptionFlowDto[];
}

export interface OptionFlowDto {
  optionId: number;
  optionText: string;
  next?: NextQuestionDeterminant | null;
}
```

---

### Question Types

```typescript
// Question entity
export interface Question {
  id: number;
  surveyId: number;
  questionText: string;
  questionType: QuestionType;
  orderIndex: number;
  isRequired: boolean;
  options: string[] | null;
  optionDetails?: QuestionOption[] | null;
  defaultNext?: NextQuestionDeterminant | null; // NEW
  supportsBranching?: boolean;
  mediaContent?: string | null;
  createdAt: string;
  updatedAt: string;
}

// Question option
export interface QuestionOption {
  id: number;
  text: string;
  orderIndex: number;
  next?: NextQuestionDeterminant | null; // NEW
}
```

---

## Debugging Tips

### 1. Console Logging

```typescript
// Log transformation input and output
console.log('Input (API):', determinant);
const dropdownValue = transformApiToDropdown(determinant);
console.log('Output (Dropdown):', dropdownValue);
```

### 2. Browser DevTools

- Check Network tab for API requests/responses
- Verify payload structure in Request Payload
- Check response in Preview tab

### 3. Type Guards

```typescript
// Helper function to check if value is valid determinant
function isValidDeterminant(value: any): value is NextQuestionDeterminant {
  return value &&
    typeof value === 'object' &&
    'type' in value &&
    (value.type === 'EndSurvey' || value.type === 'GoToQuestion');
}

// Usage
if (isValidDeterminant(config.defaultNext)) {
  // TypeScript knows this is NextQuestionDeterminant
}
```

---

## Testing Scenarios

### Scenario 1: End Survey Flow

```typescript
// Input from UI
const dropdownValue = -1; // User selected "End Survey"

// Transform to API
const determinant = { type: 'EndSurvey' as const };

// Send to API
await questionFlowService.updateQuestionFlow(surveyId, questionId, {
  defaultNext: determinant
});

// Verify in API response
const config = await questionFlowService.getQuestionFlow(surveyId, questionId);
expect(config.defaultNext?.type).toBe('EndSurvey');
```

---

### Scenario 2: Go To Question Flow

```typescript
// Input from UI
const dropdownValue = 5; // User selected Question 5

// Transform to API
const determinant = { type: 'GoToQuestion' as const, questionId: 5 };

// Send to API
await questionFlowService.updateQuestionFlow(surveyId, questionId, {
  defaultNext: determinant
});

// Verify in API response
const config = await questionFlowService.getQuestionFlow(surveyId, questionId);
expect(config.defaultNext?.type).toBe('GoToQuestion');
expect(config.defaultNext?.questionId).toBe(5);
```

---

### Scenario 3: Branching Flow (SingleChoice)

```typescript
// Input from UI
const optionFlows = {
  101: 5,   // Option 101 → Question 5
  102: -1,  // Option 102 → End Survey
  103: 7,   // Option 103 → Question 7
};

// Transform to API
const transformedFlows = Object.fromEntries(
  Object.entries(optionFlows).map(([optionId, nextId]) => [
    optionId,
    nextId === -1
      ? { type: 'EndSurvey' as const }
      : { type: 'GoToQuestion' as const, questionId: nextId }
  ])
);

// Send to API
await questionFlowService.updateQuestionFlow(surveyId, questionId, {
  optionNextQuestions: transformedFlows
});

// Verify in API response
const config = await questionFlowService.getQuestionFlow(surveyId, questionId);
expect(config.optionFlows[0].next?.type).toBe('GoToQuestion');
expect(config.optionFlows[1].next?.type).toBe('EndSurvey');
```

---

## Summary

### Key Takeaways

1. **Always use value objects** when communicating with API
2. **Use -1 convention** for "End Survey" in UI dropdowns
3. **Transform bidirectionally** between API and UI state
4. **Use 'as const'** to ensure type literal narrowing
5. **Handle null explicitly** - don't assume defaults
6. **Log transformations** during development

### Quick Checklist

- [ ] Import `NextQuestionDeterminant` from `@/types`
- [ ] Use Pattern 1 for reading API responses
- [ ] Use Pattern 2 for writing API requests
- [ ] Include null checks in transformation logic
- [ ] Add 'as const' to type literals
- [ ] Test both "End Survey" and "Go To Question" scenarios
- [ ] Verify API payload in browser DevTools

---

**Document Version**: 1.0
**Last Updated**: 2025-11-23
**Related Documentation**:
- [Frontend CLAUDE.md](../../frontend/CLAUDE.md)
- [API Layer CLAUDE.md](../../src/SurveyBot.API/CLAUDE.md)
- [Frontend Implementation Report](../../FRONTEND_NEXTQUESTIONDETERMINANT_IMPLEMENTATION_REPORT.md)
