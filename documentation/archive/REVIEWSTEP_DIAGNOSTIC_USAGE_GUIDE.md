# ReviewStep Diagnostic Logging - Usage Guide

**Date**: 2025-11-23
**Purpose**: Quick reference for using diagnostic logging in ReviewStep.tsx

---

## Quick Start

### 1. Open Browser DevTools

- **Chrome/Edge**: Press `F12` or `Ctrl+Shift+I`
- **Firefox**: Press `F12` or `Ctrl+Shift+K`
- Navigate to **Console** tab

### 2. Create Test Survey with Conditional Flow

**Scenario A: Simple Branching**
```
Question 1: "Are you satisfied?" (SingleChoice)
  Option 0: "Yes" → End Survey (0)
  Option 1: "No" → Question 2

Question 2: "What can we improve?" (Text)
  Default Flow → End Survey (0)
```

**Scenario B: Complex Multi-Path**
```
Question 1: "Choose category" (SingleChoice)
  Option 0: "Product" → Question 2
  Option 1: "Service" → Question 3
  Option 2: "Other" → End Survey (0)

Question 2: "Rate product" (Rating)
  Default Flow → End Survey (0)

Question 3: "Rate service" (Rating)
  Default Flow → End Survey (0)
```

### 3. Publish Survey

1. Fill in basic info (Step 1)
2. Add questions with flow configuration (Step 2)
3. Navigate to Review step (Step 3)
4. Click **"Publish Survey"**
5. Confirm in dialog

### 4. Watch Console Output

The console will display structured, hierarchical logs showing:
- Survey metadata
- Question creation progress
- UUID → Database ID mappings
- Option ID mappings
- Flow transformations
- API payloads
- Success/failure status

---

## Reading the Logs

### Understanding the Structure

```
🚀 SURVEY PUBLISH STARTED                  ← Start marker
  └─ Survey metadata

✅ Survey created/updated                    ← Survey entity created

📝 PASS 1: Creating Questions                ← Question creation phase
  ├─ Question metadata preview
  ├─ Creating question 1/N
  ├─ Creating question 2/N
  └─ ✅ PASS 1 COMPLETE
      └─ [TABLE] UUID → Database ID mapping

🔍 PASS 1.5: Fetching Option Database IDs   ← Option ID retrieval
  ├─ Question 148: { options: [...] }
  └─ ✅ PASS 1.5 complete

🔄 PASS 2: UUID → Database ID Transformations ← Flow configuration
  ├─ Question 1
  │   ├─ Database ID: 148
  │   ├─ Default Flow Transformation
  │   ├─ Option Flow Transformations
  │   │   ├─ Option 0: { ... }
  │   │   └─ Option 1: { ... }
  │   └─ 🌐 API REQUEST
  │       ├─ Endpoint
  │       ├─ Payload
  │       └─ ✅ Response
  └─ ✅ PASS 2 complete

📊 PUBLISH SUMMARY                           ← Final summary
  └─ Statistics

🎉 SURVEY PUBLISH COMPLETED                  ← Success marker
```

### Key Indicators

| Symbol | Meaning |
|--------|---------|
| 🚀 | Process started |
| 📝 | Creating/writing data |
| 🔍 | Fetching/reading data |
| 🔄 | Transforming data |
| 🌐 | API request |
| ✅ | Success |
| ❌ | Error |
| ⚠️ | Warning/fallback |
| ℹ️ | Information |
| 📊 | Summary/statistics |
| 🎉 | Process completed |

---

## Diagnostic Scenarios

### Scenario 1: FK Constraint Violation

**Symptom**: API returns 400 error with foreign key constraint message

**What to Look For**:
1. Check **API REQUEST** payload
2. Verify `optionNextQuestions` uses **database IDs**, not indexes
3. Example of CORRECT payload:
   ```json
   {
     "optionNextQuestions": {
       "201": 149,  ← Option DB ID 201 → Question DB ID 149
       "202": 0     ← Option DB ID 202 → End Survey
     }
   }
   ```
4. Example of INCORRECT payload:
   ```json
   {
     "optionNextQuestions": {
       "0": 149,    ← ❌ WRONG: Using option index instead of DB ID
       "1": 0
     }
   }
   ```

**Diagnostic Steps**:
1. Find the failed API request in console
2. Expand **"Payload"** section
3. Look at `optionNextQuestions` keys
4. Cross-reference with **PASS 1.5** option mappings
5. Verify keys match option database IDs from PASS 1.5

### Scenario 2: UUID Not Found

**Symptom**: Console shows `❌ UUID NOT FOUND in questionIdMap!`

**What to Look For**:
1. Check if UUID exists in **PASS 1 COMPLETE** table
2. Look for the missing UUID in available UUIDs list
3. Check if question was created successfully

**Example**:
```
Default Flow Transformation:
  Original Value: uuid-xyz-789
  UUID Lookup Result: undefined
  ❌ UUID NOT FOUND in questionIdMap!
  Available UUIDs: ['uuid-abc-123', 'uuid-def-456']
  ⚠️ FALLBACK TRIGGERED: Setting to 0 (end survey)
```

**Resolution**: UUID mismatch indicates frontend state issue - question wasn't created or UUID wasn't stored correctly

### Scenario 3: Option Index Not Found

**Symptom**: Console shows `⚠️ OPTION SKIPPED: Index not in mapping`

**What to Look For**:
1. Check **PASS 1.5** option mappings for the question
2. Verify option count matches expected
3. Check available indexes vs. attempted index

**Example**:
```
Option 3:
  Option Index: 3
  Option Database ID: ❌ NOT FOUND
  ⚠️ OPTION SKIPPED: Index not in mapping
  Available indexes: [0, 1, 2]
```

**Resolution**: Option index mismatch - frontend is trying to configure flow for option 3, but question only has options 0-2

### Scenario 4: API Payload Verification

**What to Check**:
```
🌐 API REQUEST: Update Flow for Question 148
  Endpoint: PUT /api/surveys/53/questions/148/flow
  Payload: {
    defaultNextQuestionId: 149,
    optionNextQuestions: { 201: 150, 202: 0 },
    _analysis: {
      defaultFlowType: "149 (specific question)",  ← Human-readable
      optionFlowCount: 2,                          ← Should match options configured
      optionFlowDetails: [
        { optionDbId: "201", nextQuestionId: 150, flowType: "question 150" },
        { optionDbId: "202", nextQuestionId: 0, flowType: "end survey" }
      ]
    }
  }
```

**Validation Checklist**:
- ✅ `defaultNextQuestionId` is a number or 0 (not UUID string)
- ✅ `optionNextQuestions` keys are option **database IDs** (3-digit numbers, not 0-2 indexes)
- ✅ `optionNextQuestions` values are question database IDs or 0
- ✅ `optionFlowCount` matches expected number of configured options

---

## Common Issues & Solutions

### Issue 1: All Questions Show Sequential Flow

**Symptom**: No conditional flow is applied despite configuration

**Check**:
1. **PASS 2** logs - are flow updates being skipped?
2. Look for: `"Skipping flow update for question X (no flow configured)"`

**Cause**: `defaultNextQuestionId` and `optionNextQuestions` both undefined

**Solution**: Verify QuestionEditor saved flow configuration to draft state

### Issue 2: API Returns 400 "Invalid Next Question ID"

**Symptom**: Backend rejects next question ID

**Check**:
1. **API REQUEST** payload
2. Look at `_analysis.defaultFlowType` or `optionFlowDetails`
3. Verify referenced question IDs exist

**Cause**: Sending non-existent question ID (e.g., deleted question)

**Solution**: Check **PASS 1 COMPLETE** table for valid question IDs

### Issue 3: Option Flow Not Saved

**Symptom**: Only some options have flow configured

**Check**:
1. **Option Flow Transformations** section
2. Count how many options are processed
3. Look for `⚠️ OPTION SKIPPED` warnings

**Cause**: Option index mismatch or missing option in API response

**Solution**: Verify option count in **PASS 1.5** matches QuestionEditor

---

## Performance Metrics

### Normal Timing (3-question survey)

```
📊 PUBLISH SUMMARY
  Total Questions Created: 3
  Questions with Flow Configuration: 2
  Flow Updates Successful: 2
  Flow Updates Failed: 0
  Duration: 2000ms (2.00s)
```

**Breakdown**:
- Survey creation: ~200ms
- PASS 1 (question creation): ~800ms (3 questions × ~250ms each)
- PASS 1.5 (fetch options): ~300ms
- PASS 2 (flow updates): ~600ms (2 updates × ~300ms each)
- Survey activation: ~100ms

### Warning Signs

- **Duration > 10s**: Network issues or backend performance problem
- **Flow Updates Failed > 0**: Check error details in console
- **Questions Created ≠ Expected**: Question creation failed partway

---

## Exporting Logs for Analysis

### Copy Console Output

1. **Right-click** in Console tab
2. Select **"Save as..."**
3. Save to file (e.g., `survey-publish-logs.txt`)

### Filter Console Output

1. Click **filter icon** in DevTools Console
2. Enter filter term:
   - `🚀` - Start/end markers
   - `❌` - Errors only
   - `API REQUEST` - API calls only
   - `PASS 1` or `PASS 2` - Specific phase

### Preserve Logs Across Page Reloads

1. Check **"Preserve log"** checkbox in Console tab
2. Logs will persist even after navigation/reload

---

## Next Steps After Diagnostics

### If FK Constraint Violation Found

1. **Identify**: Which API request failed?
2. **Compare**: Payload vs. PASS 1.5 option mappings
3. **Fix**: Update ReviewStep.tsx to use correct IDs
4. **Test**: Re-publish survey and verify logs

### If UUID Mismatch Found

1. **Check**: QuestionsStep.tsx draft state management
2. **Verify**: Questions are stored with consistent UUIDs
3. **Fix**: Update state management logic
4. **Test**: Create survey from scratch

### If Option Index Mismatch Found

1. **Check**: QuestionEditor option state
2. **Verify**: Options saved correctly to draft
3. **Fix**: Update QuestionEditor save logic
4. **Test**: Edit question with flow configuration

---

## Related Documentation

- **Implementation Report**: `REVIEWSTEP_DIAGNOSTIC_LOGGING_IMPLEMENTATION_REPORT.md`
- **Flow Fix Summary**: `REVIEWSTEP_FLOW_FIX_SUMMARY.md`
- **Option Validation Fix**: `OPTION_NEXT_QUESTIONS_VALIDATION_FIX_REPORT.md`
- **Frontend Documentation**: `frontend/CLAUDE.md`

---

**Quick Reference Card**

```
🚀 Start → 📝 Create Questions → ✅ UUID Mapping →
🔍 Fetch Options → ✅ Option Mapping →
🔄 Transform UUIDs → 🌐 API Requests → 📊 Summary → 🎉 Done
```

**Key Files**:
- `frontend/src/components/SurveyBuilder/ReviewStep.tsx` - Source file with logging
- Browser DevTools Console - Where logs appear

**Key Logs to Check**:
1. PASS 1 COMPLETE - UUID → Database ID mapping
2. PASS 1.5 - Option index → Database ID mapping
3. API REQUEST Payload - Verify correct IDs are sent
4. Error Details - If API call fails

---

**Last Updated**: 2025-11-23
