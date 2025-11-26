# Frontend Survey Flow Payload Construction Analysis

**Analysis Date**: 2025-11-23
**Purpose**: Comprehensive analysis of JSON payload construction for PUT /api/surveys/{id}/questions/{questionId}/flow endpoint
**Issue**: 400 Bad Request during model binding (before controller execution)

---

## Executive Summary

### Critical Findings

1. ✅ **Property Names CORRECT**: Uses `optionNextDeterminants` (not `optionNextQuestions`)
2. ✅ **Nested Property CORRECT**: Uses `nextQuestionId` (not `questionId`)
3. ✅ **Type Values CORRECT**: Sends string literals (`'GoToQuestion'`, `'EndSurvey'`)
4. ⚠️ **Potential Issue**: `optionNextDeterminants` set to `undefined` when no options configured
5. ⚠️ **Missing optionDetails Warning**: Question 5 has flow config but no `optionDetails` from API

### Likely Root Cause

**Backend may not accept `undefined` for `optionNextDeterminants`** - JavaScript/Axios omits `undefined` properties from JSON, which could cause model binding to fail if backend expects explicit `null` or empty object `{}`.

---

## Part 1: Payload Construction Analysis

### Location: ReviewStep.tsx (Lines 418-433)

```typescript
// Transform to NextQuestionDeterminant structure
const payload = {
  defaultNext: defaultNextQuestionId === undefined ? null : (
    defaultNextQuestionId === 0
      ? { type: 'EndSurvey' as const }
      : { type: 'GoToQuestion' as const, nextQuestionId: defaultNextQuestionId }
  ),
  optionNextDeterminants: optionNextQuestions ? Object.fromEntries(
    Object.entries(optionNextQuestions).map(([optionId, nextId]) => [
      optionId,
      nextId === 0
        ? { type: 'EndSurvey' as const }
        : { type: 'GoToQuestion' as const, nextQuestionId: nextId }
    ])
  ) : undefined, // ⚠️ ISSUE: undefined (not null or {})
};
```

### Payload Structure Breakdown

**Top-Level Properties:**
```typescript
{
  defaultNext: NextQuestionDeterminant | null,
  optionNextDeterminants: Record<number, NextQuestionDeterminant> | undefined
}
```

**Nested Type: NextQuestionDeterminant**
```typescript
{
  type: 'GoToQuestion' | 'EndSurvey',  // String literal
  nextQuestionId?: number | null        // Optional
}
```

---

## Part 2: DefaultNext Construction

### Construction Logic (Lines 420-424)

```typescript
defaultNext: defaultNextQuestionId === undefined ? null : (
  defaultNextQuestionId === 0
    ? { type: 'EndSurvey' as const }
    : { type: 'GoToQuestion' as const, nextQuestionId: defaultNextQuestionId }
)
```

### Scenarios

#### Scenario A: No Default Flow Configured

**Input:**
```typescript
defaultNextQuestionId === undefined
```

**Output:**
```json
{
  "defaultNext": null
}
```

**Meaning**: Sequential flow (proceed to next question by orderIndex)

---

#### Scenario B: Explicit End-of-Survey

**Input:**
```typescript
defaultNextQuestionId === 0
```

**Output:**
```json
{
  "defaultNext": {
    "type": "EndSurvey"
  }
}
```

**Notes**:
- `nextQuestionId` property **not included** (optional property omitted)
- Backend should interpret this as survey termination

---

#### Scenario C: Flow to Specific Question

**Input:**
```typescript
defaultNextQuestionId === 5
```

**Output:**
```json
{
  "defaultNext": {
    "type": "GoToQuestion",
    "nextQuestionId": 5
  }
}
```

**Notes**:
- Both `type` and `nextQuestionId` present
- `nextQuestionId` is the database ID (not UUID)

---

## Part 3: OptionNextDeterminants Construction

### Construction Logic (Lines 425-432)

```typescript
optionNextDeterminants: optionNextQuestions ? Object.fromEntries(
  Object.entries(optionNextQuestions).map(([optionId, nextId]) => [
    optionId,
    nextId === 0
      ? { type: 'EndSurvey' as const }
      : { type: 'GoToQuestion' as const, nextQuestionId: nextId }
  ])
) : undefined
```

### Scenarios

#### Scenario A: No Option Flows

**Input:**
```typescript
optionNextQuestions is undefined/null/falsy
```

**Output:**
```json
{
  "optionNextDeterminants": undefined  // ⚠️ OMITTED FROM JSON
}
```

**Critical Issue**:
- JavaScript `undefined` causes Axios to **omit the property entirely** from JSON
- Backend may expect `null` or `{}` instead
- **This could be the root cause of 400 Bad Request**

---

#### Scenario B: Option Flows Configured

**Input:**
```typescript
optionNextQuestions = {
  1: 0,   // Option DB ID 1 → End Survey
  2: 5    // Option DB ID 2 → Question 5
}
```

**Output:**
```json
{
  "optionNextDeterminants": {
    "1": {
      "type": "EndSurvey"
    },
    "2": {
      "type": "GoToQuestion",
      "nextQuestionId": 5
    }
  }
}
```

**Notes**:
- Keys are **option database IDs** (numbers as strings in JSON)
- Each value is a `NextQuestionDeterminant` object
- Property name is **`optionNextDeterminants`** (correct spelling)

---

## Part 4: Property Names Verification

### ✅ All Property Names CORRECT

| Level | Property Name | Correct | Notes |
|-------|---------------|---------|-------|
| **Top-Level** | `defaultNext` | ✅ | Matches C# DTO |
| **Top-Level** | `optionNextDeterminants` | ✅ | Correctly spelled (not `optionNextQuestions`) |
| **Nested** | `type` | ✅ | String literal type |
| **Nested** | `nextQuestionId` | ✅ | NOT `questionId` |

### Case Sensitivity Check

- ✅ `optionNextDeterminants` - camelCase (JavaScript convention)
- ✅ Backend C# DTO uses `OptionNextDeterminants` - PascalCase
- ✅ ASP.NET Core auto-converts camelCase ↔ PascalCase with default JSON serialization

---

## Part 5: Type Property Serialization

### Type Values: String Literals (NOT Enums)

**Frontend Type Definition** (`types/index.ts:236`):
```typescript
export type NextStepType = 'GoToQuestion' | 'EndSurvey';
```

**Payload Construction** (ReviewStep.tsx:422, 423, 429, 430):
```typescript
{ type: 'EndSurvey' as const }
{ type: 'GoToQuestion' as const }
```

**Actual JSON Sent**:
```json
{
  "type": "EndSurvey"
}
```
OR
```json
{
  "type": "GoToQuestion"
}
```

### ✅ Correct Format

- Sends **string literals**: `"EndSurvey"`, `"GoToQuestion"`
- NOT enum values: `0`, `1`
- Backend C# enum should auto-deserialize from string names

---

## Part 6: Complete Request Flow

### End-to-End Flow

```
User clicks "Publish Survey" in ReviewStep.tsx
        ↓
PASS 1: Question Creation (Lines 172-218)
  - Create questions WITHOUT flow configuration
  - Get database IDs for each question
  - Build questionIdMap: UUID → DB ID
        ↓
PASS 1.5: Fetch Option IDs (Lines 229-271)
  - Fetch questions with optionDetails from API
  - ⚠️ WARNING: "Question 5 has flow config but no optionDetails from API"
  - Build optionMappings: questionDbId → Map<optionIndex, optionDbId>
        ↓
PASS 2: UUID → DB ID Transformation (Lines 274-409)
  - Transform draft UUIDs to database IDs
  - Build defaultNextQuestionId (number | null | undefined)
  - Build optionNextQuestions: Record<optionDbId, nextQuestionDbId>
        ↓
Flow Update API Call (Lines 414-476)
  - Construct payload (Lines 418-433)
  - Log payload (Lines 435-451)
  - Call questionFlowService.updateQuestionFlow() (Lines 454-457)
        ↓
Service Layer: questionFlowService.ts
  - PUT /surveys/{surveyId}/questions/{questionId}/flow
  - Send payload as JSON
        ↓
HTTP Client: api.ts
  - Axios serializes payload with JSON.stringify()
  - undefined properties OMITTED from JSON
  - Includes Authorization header
  - Includes ngrok-skip-browser-warning header
        ↓
Backend API receives JSON
  - Model binding attempts to deserialize to UpdateQuestionFlowDto
  - ⚠️ 400 Bad Request - Binding fails
```

---

## Part 7: Service Layer Analysis

### File: `questionFlowService.ts`

#### Method Definition (Lines 79-93)

```typescript
async updateQuestionFlow(
  surveyId: string | number,
  questionId: string | number,
  dto: UpdateQuestionFlowDto
): Promise<ConditionalFlowDto> {
  try {
    const response = await api.put<ApiResponse<ConditionalFlowDto>>(
      `${this.basePath}/${surveyId}/questions/${questionId}/flow`,
      dto
    );
    return response.data.data!;
  } catch (error) {
    console.error('Error updating question flow:', error);
    throw error;
  }
}
```

#### Type Definition (`types/index.ts:264-267`)

```typescript
export interface UpdateQuestionFlowDto {
  defaultNext?: NextQuestionDeterminant | null;
  optionNextDeterminants?: Record<number, NextQuestionDeterminant>;
}
```

### ✅ Type Safety Verified

- Method signature matches actual usage
- Type definition matches payload structure
- Property names correct: `defaultNext`, `optionNextDeterminants`

---

## Part 8: Type Definitions Analysis

### File: `types/index.ts`

#### NextQuestionDeterminant Interface (Lines 239-242)

```typescript
export interface NextQuestionDeterminant {
  type: NextStepType;
  nextQuestionId?: number | null;  // ✅ Correct property name
}
```

**Verification**:
- ✅ Property name is `nextQuestionId` (NOT `questionId`)
- ✅ Optional property (can be omitted for EndSurvey)
- ✅ Can be `number | null`

---

#### NextStepType Definition (Line 236)

```typescript
export type NextStepType = 'GoToQuestion' | 'EndSurvey';
```

**Verification**:
- ✅ String literal union type
- ✅ Values: `'GoToQuestion'`, `'EndSurvey'` (NOT enum numbers)

---

#### UpdateQuestionFlowDto Interface (Lines 264-267)

```typescript
export interface UpdateQuestionFlowDto {
  defaultNext?: NextQuestionDeterminant | null;
  optionNextDeterminants?: Record<number, NextQuestionDeterminant>;  // ✅ Correct name
}
```

**Verification**:
- ✅ Property name is `optionNextDeterminants` (NOT `optionNextQuestions`)
- ✅ Both properties are optional (`?`)
- ✅ `defaultNext` can be `null` explicitly
- ⚠️ `optionNextDeterminants` can be `undefined` (may cause issue)

---

## Part 9: Example Payloads

### Example 1: SingleChoice Question with 2 Options

**Scenario**: Question 9 (SingleChoice) with 2 options
- Option 1 (DB ID: 1) → Question 10
- Option 2 (DB ID: 2) → End Survey

**Payload Constructed**:
```typescript
{
  defaultNext: null,
  optionNextDeterminants: {
    1: 0,   // Option 1 → End Survey (0)
    2: 10   // Option 2 → Question 10
  }
}
```

**After Transformation (Lines 418-433)**:
```typescript
{
  defaultNext: null,
  optionNextDeterminants: {
    1: { type: 'EndSurvey' },
    2: { type: 'GoToQuestion', nextQuestionId: 10 }
  }
}
```

**Actual JSON Sent**:
```json
{
  "defaultNext": null,
  "optionNextDeterminants": {
    "1": {
      "type": "EndSurvey"
    },
    "2": {
      "type": "GoToQuestion",
      "nextQuestionId": 10
    }
  }
}
```

---

### Example 2: Text Question with Default Next

**Scenario**: Question 11 (Text) with default next → Question 12

**Payload Constructed**:
```typescript
{
  defaultNext: { type: 'GoToQuestion', nextQuestionId: 12 },
  optionNextDeterminants: undefined
}
```

**Actual JSON Sent**:
```json
{
  "defaultNext": {
    "type": "GoToQuestion",
    "nextQuestionId": 12
  }
}
```

**⚠️ Critical Issue**: `optionNextDeterminants` property **OMITTED** because it's `undefined`

**Backend may expect**:
```json
{
  "defaultNext": {
    "type": "GoToQuestion",
    "nextQuestionId": 12
  },
  "optionNextDeterminants": null  // or {}
}
```

---

### Example 3: Text Question Ending Survey

**Scenario**: Question 12 (Text) with default → End Survey

**Payload Constructed**:
```typescript
{
  defaultNext: { type: 'EndSurvey' },
  optionNextDeterminants: undefined
}
```

**Actual JSON Sent**:
```json
{
  "defaultNext": {
    "type": "EndSurvey"
  }
}
```

**⚠️ Same Issue**: `optionNextDeterminants` omitted

---

### Example 4: Sequential Flow (No Config)

**Scenario**: Question with no explicit flow configured

**Payload Constructed**:
```typescript
{
  defaultNext: null,
  optionNextDeterminants: undefined
}
```

**Actual JSON Sent**:
```json
{
  "defaultNext": null
}
```

**⚠️ Same Issue**: `optionNextDeterminants` omitted

---

## Part 10: Console Logging Analysis

### Log Output (Lines 435-451)

```typescript
console.group(`🌐 API REQUEST: Update Flow for Question ${questionDbId}`);
console.log('Endpoint:', `PUT /api/surveys/${survey.id}/questions/${questionDbId}/flow`);
console.log('Payload:', {
  defaultNext: payload.defaultNext,
  optionNextDeterminants: payload.optionNextDeterminants,
  _analysis: {
    defaultFlowType: !payload.defaultNext ? 'null (sequential)' :
                      payload.defaultNext.type === 'EndSurvey' ? 'EndSurvey' :
                      `GoToQuestion ${payload.defaultNext.nextQuestionId}`,
    optionFlowCount: payload.optionNextDeterminants ? Object.keys(payload.optionNextDeterminants).length : 0,
    optionFlowDetails: payload.optionNextDeterminants ? Object.entries(payload.optionNextDeterminants).map(([k, v]) => ({
      optionDbId: k,
      next: v,
      flowType: v.type === 'EndSurvey' ? 'end survey' : `question ${v.nextQuestionId}`,
    })) : [],
  },
});
```

### Analysis Object Breakdown

**defaultFlowType**:
- ✅ Correctly reads `payload.defaultNext.type`
- ✅ Correctly reads `payload.defaultNext.nextQuestionId`

**optionFlowCount**:
- ✅ Correctly counts keys in `payload.optionNextDeterminants`

**optionFlowDetails**:
- ✅ Correctly maps option flows
- ✅ Uses property name `optionDbId` (key)
- ✅ Reads `v.type` and `v.nextQuestionId`

### ✅ Logging CORRECT

The logging correctly analyzes the payload structure, proving the payload is constructed as intended.

---

## Part 11: Missing optionDetails Issue

### Warning Location (Lines 262-264)

```typescript
if (!q.optionDetails || q.optionDetails.length === 0) {
  console.warn(`⚠️ Question ${q.id} has flow config but no optionDetails from API`);
}
```

### Root Cause Analysis

**What causes the warning?**

1. **Question created in PASS 1** without flow configuration
2. **QuestionService.CreateQuestion** returns Question DTO
3. **Question DTO SHOULD include optionDetails** with database IDs for each option
4. **IF optionDetails is missing/empty**, warning is logged

**Impact on Flow Configuration**:

If `optionDetails` is missing:
- ❌ Cannot build `optionIdMap` (option index → option DB ID)
- ❌ `optionNextQuestions` keys will be **incorrect** (might use indexes instead of IDs)
- ❌ Backend will receive wrong option IDs in `optionNextDeterminants`

**Example of Incorrect Mapping**:

```typescript
// WRONG: Using option index as key
optionNextDeterminants: {
  0: { type: 'GoToQuestion', nextQuestionId: 5 },  // 0 is index, not DB ID
  1: { type: 'EndSurvey' }
}

// CORRECT: Using option database ID as key
optionNextDeterminants: {
  10: { type: 'GoToQuestion', nextQuestionId: 5 },  // 10 is option DB ID
  11: { type: 'EndSurvey' }
}
```

### Backend Issue?

**Is this a backend problem?**

**YES** - If the backend `QuestionService.CreateQuestion` does NOT:
1. Create `QuestionOption` entities for each option text
2. Return `optionDetails` array in the Question DTO

**Expected Backend Behavior**:
```json
{
  "id": 5,
  "questionText": "What's your favorite color?",
  "questionType": 1,
  "options": ["Red", "Blue", "Green"],
  "optionDetails": [
    { "id": 10, "text": "Red", "orderIndex": 0 },
    { "id": 11, "text": "Blue", "orderIndex": 1 },
    { "id": 12, "text": "Green", "orderIndex": 2 }
  ]
}
```

**If optionDetails is missing**, this is a **backend bug** in QuestionService or mapping.

---

## Part 12: Axios Serialization

### Configuration (api.ts:9-16)

```typescript
const api: AxiosInstance = axios.create({
  baseURL: getApiBaseUrl(),
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
    'ngrok-skip-browser-warning': 'true',
  },
});
```

### Default Axios JSON Serialization

**Behavior**:
1. Uses `JSON.stringify()` on payload
2. `undefined` properties → **OMITTED** from JSON
3. `null` properties → sent as `null`
4. Property names → preserved as-is (camelCase)
5. No custom transformers configured

### Critical Issue: undefined vs null

**Example Payload**:
```typescript
{
  defaultNext: { type: 'GoToQuestion', nextQuestionId: 5 },
  optionNextDeterminants: undefined
}
```

**JSON Sent**:
```json
{
  "defaultNext": {
    "type": "GoToQuestion",
    "nextQuestionId": 5
  }
}
```

**optionNextDeterminants is COMPLETELY OMITTED**

### Backend Expectation Mismatch?

**If backend C# DTO expects**:
```csharp
public class UpdateQuestionFlowDto
{
    public NextQuestionDeterminant? DefaultNext { get; set; }
    public Dictionary<int, NextQuestionDeterminant>? OptionNextDeterminants { get; set; }
}
```

**And model binding requires BOTH properties to be present** (even if null):
- ❌ Missing `optionNextDeterminants` property causes binding failure
- ✅ Sending `"optionNextDeterminants": null` would succeed

---

## Part 13: Comparison Readiness

### Frontend Payload Structure (TypeScript)

```typescript
interface UpdateQuestionFlowDto {
  defaultNext?: NextQuestionDeterminant | null;
  optionNextDeterminants?: Record<number, NextQuestionDeterminant>;
}

interface NextQuestionDeterminant {
  type: 'GoToQuestion' | 'EndSurvey';
  nextQuestionId?: number | null;
}
```

### Expected Backend DTO (C#)

```csharp
public class UpdateQuestionFlowDto
{
    public NextQuestionDeterminant? DefaultNext { get; set; }
    public Dictionary<int, NextQuestionDeterminant>? OptionNextDeterminants { get; set; }
}

public class NextQuestionDeterminant
{
    public NextStepType Type { get; set; }
    public int? NextQuestionId { get; set; }
}

public enum NextStepType
{
    GoToQuestion,
    EndSurvey
}
```

### Property Name Comparison

| TypeScript Property | C# Property | Match? |
|---------------------|-------------|--------|
| `defaultNext` | `DefaultNext` | ✅ (auto-converted) |
| `optionNextDeterminants` | `OptionNextDeterminants` | ✅ (auto-converted) |
| `type` | `Type` | ✅ (auto-converted) |
| `nextQuestionId` | `NextQuestionId` | ✅ (auto-converted) |

### Data Type Comparison

| TypeScript Type | C# Type | Match? |
|-----------------|---------|--------|
| `'GoToQuestion' \| 'EndSurvey'` | `NextStepType enum` | ✅ (string → enum) |
| `number \| null` | `int?` | ✅ |
| `Record<number, T>` | `Dictionary<int, T>` | ✅ |
| `T \| undefined` | `T?` (nullable) | ⚠️ **ISSUE** |

### Critical Mismatch

**TypeScript**:
```typescript
optionNextDeterminants?: Record<number, NextQuestionDeterminant>
```
- Can be `undefined` → **OMITTED FROM JSON**

**C#**:
```csharp
public Dictionary<int, NextQuestionDeterminant>? OptionNextDeterminants { get; set; }
```
- Expects `null` or `Dictionary<int, T>` → **REQUIRES PROPERTY IN JSON**

---

## Recommendations

### Immediate Fix: Change undefined to null

**File**: `ReviewStep.tsx:432`

**Current**:
```typescript
optionNextDeterminants: optionNextQuestions ? Object.fromEntries(...) : undefined
```

**Change to**:
```typescript
optionNextDeterminants: optionNextQuestions ? Object.fromEntries(...) : null
```

**Result**:
```json
{
  "defaultNext": { "type": "GoToQuestion", "nextQuestionId": 5 },
  "optionNextDeterminants": null  // ✅ Property present with null value
}
```

### Backend Investigation

**Check if backend requires**:
1. Both properties present in JSON (even if null)
2. Property binding validation
3. Custom model binder that fails on missing properties

### Address Missing optionDetails

**Investigate backend**:
1. Does `QuestionService.CreateQuestion` create `QuestionOption` entities?
2. Does the Question DTO mapping include `optionDetails`?
3. Is `optionDetails` populated in the GET questions endpoint?

**If missing**, this is a separate backend bug affecting flow configuration for branching questions.

---

## Conclusion

### ✅ What's Correct

1. Property names: `optionNextDeterminants`, `nextQuestionId`
2. Type values: String literals (`'GoToQuestion'`, `'EndSurvey'`)
3. Payload structure matches TypeScript types
4. Service layer and HTTP client configured correctly

### ⚠️ Likely Root Cause of 400 Error

**`optionNextDeterminants: undefined` causes property to be omitted from JSON**

Backend model binding may **require the property to be present** with `null` value instead.

### 🔧 Recommended Fix

Change line 432 in `ReviewStep.tsx`:
```typescript
optionNextDeterminants: optionNextQuestions ? Object.fromEntries(...) : null
```

This ensures the property is **always present in JSON** with either a dictionary or `null`.

---

**End of Analysis**
