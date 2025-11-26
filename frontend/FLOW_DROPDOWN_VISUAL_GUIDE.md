# Flow Dropdown Integration - Visual Guide

**Date**: 2025-11-22

---

## Visual Examples

### Scenario: Survey with 4 Questions

**Questions in Survey**:
1. Q1: What is your name? (Text)
2. Q2: Are you satisfied with our service? (SingleChoice: Yes/No/Maybe)
3. Q3: Please rate your experience (Rating 1-5)
4. Q4: Any additional comments? (Text)

---

## SingleChoice Question - Editing Q2

When editing Q2 "Are you satisfied with our service?" with 3 options:

```
┌─────────────────────────────────────────────────────────┐
│ Conditional Flow                                        │
│                                                         │
│ Configure which question to show next based on the      │
│ respondent's answer.                                    │
│                                                         │
│ ─────────────────────────────────────────────────────── │
│                                                         │
│ Next question after "Yes"                               │
│ ┌─────────────────────────────────────────────────────┐ │
│ │ End Survey                                ▼         │ │
│ ├─────────────────────────────────────────────────────┤ │
│ │ Q1: What is your name?                              │ │
│ │ Q3: Please rate your experience                     │ │
│ │ Q4: Any additional comments?                        │ │
│ └─────────────────────────────────────────────────────┘ │
│                                                         │
│ Next question after "No"                                │
│ ┌─────────────────────────────────────────────────────┐ │
│ │ End Survey                                ▼         │ │
│ ├─────────────────────────────────────────────────────┤ │
│ │ Q1: What is your name?                              │ │
│ │ Q3: Please rate your experience                     │ │
│ │ Q4: Any additional comments?                        │ │
│ └─────────────────────────────────────────────────────┘ │
│                                                         │
│ Next question after "Maybe"                             │
│ ┌─────────────────────────────────────────────────────┐ │
│ │ End Survey                                ▼         │ │
│ ├─────────────────────────────────────────────────────┤ │
│ │ Q1: What is your name?                              │ │
│ │ Q3: Please rate your experience                     │ │
│ │ Q4: Any additional comments?                        │ │
│ └─────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
```

**Notes**:
- Q2 (current question) is **excluded** from all dropdowns
- Each option has its own independent dropdown
- Can create branching logic: Yes → Q4, No → Q3, Maybe → End

---

## Rating Question - Editing Q3

When editing Q3 "Please rate your experience":

```
┌─────────────────────────────────────────────────────────┐
│ Conditional Flow                                        │
│                                                         │
│ Configure which question to show next based on the      │
│ respondent's answer.                                    │
│                                                         │
│ ─────────────────────────────────────────────────────── │
│                                                         │
│ Next question after any rating                          │
│ ┌─────────────────────────────────────────────────────┐ │
│ │ End Survey                                ▼         │ │
│ ├─────────────────────────────────────────────────────┤ │
│ │ Q1: What is your name?                              │ │
│ │ Q2: Are you satisfied with our service?             │ │
│ │ Q4: Any additional comments?                        │ │
│ └─────────────────────────────────────────────────────┘ │
│                                                         │
│ ℹ Select "End Survey" to complete the survey after     │
│   this question, or choose the next question to        │
│   continue the flow.                                    │
└─────────────────────────────────────────────────────────┘
```

**Notes**:
- Single dropdown for all rating values
- Q3 (current question) is **excluded**
- All ratings (1-5 stars) navigate to the same next question

---

## Text Question - Editing Q1

When editing Q1 "What is your name?":

```
┌─────────────────────────────────────────────────────────┐
│ Next Question                                           │
│                                                         │
│ Which question should appear next?                      │
│ ┌─────────────────────────────────────────────────────┐ │
│ │ End Survey                                ▼         │ │
│ ├─────────────────────────────────────────────────────┤ │
│ │ Q2: Are you satisfied with our service?             │ │
│ │ Q3: Please rate your experience                     │ │
│ │ Q4: Any additional comments?                        │ │
│ └─────────────────────────────────────────────────────┘ │
│                                                         │
│ All answers to this question will navigate to the      │
│ selected question.                                      │
└─────────────────────────────────────────────────────────┘
```

**Notes**:
- Single dropdown for all text answers
- Q1 (current question) is **excluded**
- No branching - all answers go to same next question

---

## Edge Case: Only One Question in Survey

When editing the **only question** in the survey:

```
┌─────────────────────────────────────────────────────────┐
│ Next Question                                           │
│                                                         │
│ Which question should appear next?                      │
│ ┌─────────────────────────────────────────────────────┐ │
│ │ End Survey                                ▼         │ │
│ ├─────────────────────────────────────────────────────┤ │
│ │ No other questions available                        │ │  ← Disabled
│ └─────────────────────────────────────────────────────┘ │
│                                                         │
│ All answers to this question will navigate to the      │
│ selected question.                                      │
└─────────────────────────────────────────────────────────┘
```

**Notes**:
- Only "End Survey" is selectable
- "No other questions available" is **disabled** (gray)
- User must add more questions to create flow

---

## HTML Tag Stripping Examples

The dropdown automatically strips HTML tags from rich text questions:

### Example 1: Bold Text
**Question Text**: `<p>What is your <strong>favorite</strong> color?</p>`
**Dropdown Display**: `Q2: What is your favorite color?`

### Example 2: Links
**Question Text**: `<p>Visit our <a href="...">website</a> and tell us what you think</p>`
**Dropdown Display**: `Q3: Visit our website and tell us what you think`

### Example 3: Multiple Paragraphs
**Question Text**: `<p>First paragraph</p><p>Second paragraph</p>`
**Dropdown Display**: `Q4: First paragraphSecond paragraph`

---

## Text Truncation Examples

Questions longer than 50 characters are truncated:

### Example 1: Long Question (55 chars)
**Full Text**: `What is your overall satisfaction with our service?`
**Dropdown Display**: `Q1: What is your overall satisfaction with our ser...`

### Example 2: Very Long Question (120 chars)
**Full Text**: `Please describe in detail your experience with our customer service team and how we can improve our support services`
**Dropdown Display**: `Q2: Please describe in detail your experience with...`

### Example 3: Short Question (20 chars)
**Full Text**: `What is your name?`
**Dropdown Display**: `Q3: What is your name?` (no truncation)

---

## Question Numbering

Questions are displayed with 1-indexed numbers matching the UI:

**Internal `orderIndex`**: 0, 1, 2, 3
**Display Numbers**: Q1, Q2, Q3, Q4

**Code**: `Q{q.orderIndex + 1}`

---

## Interaction Flow

1. **User clicks "Edit" on Q2** (SingleChoice)
2. **Question Editor opens** with Q2 data
3. **Scroll to Conditional Flow section**
4. **See dropdowns for each option** (Yes, No, Maybe)
5. **Click dropdown for "Yes" option**
6. **Dropdown expands** showing:
   - End Survey
   - Q1: What is your name?
   - Q3: Please rate your experience
   - Q4: Any additional comments?
7. **Select "Q4: Any additional comments?"**
8. **Dropdown closes** with selected value
9. **Repeat for other options** (No, Maybe)
10. **Click "Update Question"**
11. **Flow configuration saved** to question draft

---

## Benefits of This Implementation

### User Experience
- **Clear context**: See actual question text, not generic placeholders
- **Visual hierarchy**: Question numbers make order obvious
- **No confusion**: Current question excluded prevents mistakes
- **Readable**: HTML stripped, long text truncated

### Developer Experience
- **Type-safe**: Full TypeScript support
- **Reusable**: Helper function `getAvailableNextQuestions()`
- **Maintainable**: Single source of truth (`allQuestions` prop)
- **Testable**: Pure function for filtering questions

### Future-Proof
- **Scalable**: Works with any number of questions
- **Flexible**: Easy to add icons, search, or grouping later
- **Consistent**: Same pattern across all question types

---

**Visual Guide Complete** ✅

See `FLOW_DROPDOWN_INTEGRATION_SUMMARY.md` for technical implementation details.
