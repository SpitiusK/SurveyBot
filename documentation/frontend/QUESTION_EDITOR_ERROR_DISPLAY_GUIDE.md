# Question Editor Error Display - Visual Guide

**Feature**: Enhanced validation error display in QuestionEditor dialog
**Date**: 2025-11-22
**Component**: `frontend/src/components/SurveyBuilder/QuestionEditor.tsx`

---

## Overview

The QuestionEditor dialog now provides comprehensive validation error feedback to users, making it clear when and why form submission is blocked.

---

## Error Display Locations

### 1. Top-Level Error Alert

**Location**: Top of dialog content, below title

**Appearance**:
```
┌─────────────────────────────────────────────────────┐
│ Add New Question                               [X]  │
├─────────────────────────────────────────────────────┤
│                                                     │
│ ⚠️ Please fix the following errors:                │
│                                                     │
│  • Question Text: Question text must contain at    │
│    least 5 characters of actual content            │
│  • Options: Choice questions must have 2-10        │
│    options                                         │
│                                                     │
├─────────────────────────────────────────────────────┤
│ Question Type                                       │
│ ... (rest of form)                                  │
└─────────────────────────────────────────────────────┘
```

**Features**:
- Red error alert with `⚠️` severity icon
- "Please fix the following errors:" title
- Bulleted list of all validation errors
- Field name in bold for easy scanning
- Clear, actionable error messages

---

### 2. Field-Level Error Messages

#### A. Question Text Field

**Location**: Below RichTextEditor

**Valid State**:
```
┌─────────────────────────────────────────────────────┐
│ Question Text *                                     │
│ ┌─────────────────────────────────────────────────┐ │
│ │ Enter your question with optional media...      │ │
│ └─────────────────────────────────────────────────┘ │
│ 15/500 characters (actual text content)            │
└─────────────────────────────────────────────────────┘
```

**Error State - Empty**:
```
┌─────────────────────────────────────────────────────┐
│ Question Text *                                     │
│ ┌─────────────────────────────────────────────────┐ │
│ │ Enter your question with optional media...      │ │
│ └─────────────────────────────────────────────────┘ │
│ ❌ Question text must contain at least 5           │
│    characters of actual content                    │
└─────────────────────────────────────────────────────┘
```

**Error State - Too Short**:
```
┌─────────────────────────────────────────────────────┐
│ Question Text *                                     │
│ ┌─────────────────────────────────────────────────┐ │
│ │ Hi                                              │ │
│ └─────────────────────────────────────────────────┘ │
│ ❌ Question text must be at least 5 characters     │
│    (currently 2)                                   │
└─────────────────────────────────────────────────────┘
```

**Key Features**:
- Shows actual text character count (not HTML)
- Real-time feedback as user types
- Clear indication of minimum requirement
- Distinguishes between HTML and text content

---

#### B. Options Field (Single/Multiple Choice)

**Valid State**:
```
┌─────────────────────────────────────────────────────┐
│ Answer Options                                      │
│ ┌─────────────────────────────────────────────────┐ │
│ │ 1. Option A                               [X]   │ │
│ │ 2. Option B                               [X]   │ │
│ │ 3. Option C                               [X]   │ │
│ └─────────────────────────────────────────────────┘ │
│ [+ Add Option]                                      │
│                                                     │
│ Add answer choices for respondents to select from. │
│ Drag to reorder.                                   │
└─────────────────────────────────────────────────────┘
```

**Error State - Not Enough Options**:
```
┌─────────────────────────────────────────────────────┐
│ Answer Options                                      │
│ ┌─────────────────────────────────────────────────┐ │
│ │ 1. Option A                               [X]   │ │
│ └─────────────────────────────────────────────────┘ │
│ [+ Add Option]                                      │
│                                                     │
│ ❌ At least 2 options are required for choice      │
│    questions                                       │
└─────────────────────────────────────────────────────┘
```

**Error State - Duplicate Options**:
```
┌─────────────────────────────────────────────────────┐
│ Answer Options                                      │
│ ┌─────────────────────────────────────────────────┐ │
│ │ 1. Yes                                    [X]   │ │
│ │ 2. Yes                                    [X]   │ │
│ └─────────────────────────────────────────────────┘ │
│ [+ Add Option]                                      │
│                                                     │
│ ❌ Options must be unique                          │
└─────────────────────────────────────────────────────┘
```

---

### 3. Submit Button States

#### A. Valid Form - Enabled
```
┌─────────────────────────────────────────────────────┐
│                                    [Cancel] [Add Question] │
└─────────────────────────────────────────────────────┘
```

#### B. Invalid Form - Disabled
```
┌─────────────────────────────────────────────────────┐
│                              [Cancel] [Add Question (disabled)] │
└─────────────────────────────────────────────────────┘
```
- Button appears grayed out
- Cursor shows "not-allowed" when hovering
- Tooltip could show "Fix errors to continue"

#### C. Submitting - Loading
```
┌─────────────────────────────────────────────────────┐
│                        [Cancel (disabled)] [⟳ Saving...] │
└─────────────────────────────────────────────────────┘
```
- Shows spinning circle icon
- Button text changes to "Saving..."
- Both buttons disabled during submission

---

## Error Message Catalog

### Question Text Errors

| Error Condition | Message |
|----------------|---------|
| Empty field | "Question text is required" |
| Only HTML tags | "Question text must contain at least 5 characters of actual content" |
| Too short (< 5 chars) | "Question text must be at least 5 characters (currently X)" |
| Too long (> 500 chars) | "Question text content must not exceed 500 characters" |
| HTML too long (> 5000 chars) | "Question text must not exceed 5000 characters (including formatting)" |

### Options Errors (Single/Multiple Choice)

| Error Condition | Message |
|----------------|---------|
| Less than 2 options | "At least 2 options are required for choice questions" |
| More than 10 options | "Maximum 10 options allowed" |
| Duplicate options | "Options must be unique" |
| Empty option | "Option cannot be empty" |
| Option too long | "Option must not exceed 200 characters" |

### General Errors

| Error Condition | Message |
|----------------|---------|
| No question type selected | "Question type is required" |
| Invalid conditional flow | "Invalid conditional flow configuration" |

---

## User Workflow Examples

### Scenario 1: User Tries to Submit Empty Form

**Step 1**: User clicks "Add Question" button
**Step 2**: Top error alert appears:
```
⚠️ Please fix the following errors:
 • Question Text: Question text must contain at least 5 characters of actual content
 • Options: Choice questions must have 2-10 options
```
**Step 3**: User sees submit button is disabled
**Step 4**: User fills question text
**Step 5**: Top alert updates (questionText error removed):
```
⚠️ Please fix the following errors:
 • Options: Choice questions must have 2-10 options
```
**Step 6**: User adds 2 options
**Step 7**: Error alert disappears
**Step 8**: Submit button becomes enabled
**Step 9**: User clicks "Add Question" - success!

---

### Scenario 2: User Enters Whitespace-Only Question

**Step 1**: User types spaces in RichTextEditor
**Step 2**: Character counter shows:
```
0/500 characters (actual text content)
```
**Step 3**: Error appears below editor:
```
❌ Question text must contain at least 5 characters of actual content
```
**Step 4**: Top alert also shows the error
**Step 5**: Submit button disabled
**Step 6**: User types actual text
**Step 7**: Character counter updates in real-time:
```
15/500 characters (actual text content)
```
**Step 8**: Error disappears
**Step 9**: Submit button enabled

---

### Scenario 3: User Adds Duplicate Options

**Step 1**: User selects "Single Choice" question type
**Step 2**: User adds options: "Yes", "No", "Yes"
**Step 3**: Error appears:
```
⚠️ Please fix the following errors:
 • Options: Options must be unique
```
**Step 4**: Submit button disabled
**Step 5**: User changes third option to "Maybe"
**Step 6**: Error disappears
**Step 7**: Submit button enabled

---

## Console Debug Output

When errors occur, developers can check the browser console:

### Example 1: Empty Question Text
```
Form validation state: {
  isValid: false,
  isDirty: true,
  isSubmitting: false,
  errorCount: 1,
  errors: {
    questionText: {
      message: "Question text must contain at least 5 characters of actual content",
      type: "custom"
    }
  },
  questionTextLength: 7,
  actualTextLength: 0
}

RichTextEditor onChange: {
  html: "<p></p>",
  actualText: "",
  actualLength: 0
}
```

### Example 2: Form Submission Attempt
```
Form submitted with data: {
  questionText: "What is your name?",
  questionType: 0,
  isRequired: true,
  options: [],
  defaultNextQuestionId: null,
  optionNextQuestions: {}
}

Current validation errors: {}

Saving question draft: {
  id: "a1b2c3d4-e5f6-7890-abcd-1234567890ab",
  questionText: "What is your name?",
  questionType: 0,
  isRequired: true,
  options: [],
  orderIndex: 0,
  mediaContent: null,
  defaultNextQuestionId: null,
  optionNextQuestions: {}
}
```

### Example 3: Validation Failure
```
Question text validation failed: actual text too short {
  html: "<p>Hi</p>",
  actualText: "Hi",
  actualLength: 2
}
```

---

## Accessibility Features

### Screen Reader Support

**Error Alert**:
- Uses `<Alert>` component with `severity="error"` for proper ARIA attributes
- `<AlertTitle>` provides accessible heading
- List structure (`<ul>`) is screen-reader friendly

**Field Errors**:
- `aria-describedby` links fields to error messages
- Color is not the only indicator (text messages provided)
- Focus management brings user to first error field

**Submit Button**:
- `disabled` state is announced
- Loading state announced ("Saving...")

### Keyboard Navigation

- All form controls accessible via Tab key
- Enter key submits form (if valid)
- Escape key closes dialog
- Error alert appears at top of tab order

---

## Styling

### Error Alert Colors
- Background: Light red (`error.light` from theme)
- Border: Red (`error.main`)
- Icon: Red error icon
- Text: Dark text on light background for readability

### Field Error Text
- Color: Red (`error.main`)
- Font: Caption variant (smaller than field label)
- Position: Below field, above helper text

### Submit Button Disabled State
- Background: Gray (`action.disabledBackground`)
- Text: Gray (`text.disabled`)
- Cursor: `not-allowed`
- Opacity: 0.6

---

## Implementation Notes

### Performance
- HTML stripping is fast (single DOM operation)
- Validation runs on change but is debounced in React Hook Form
- Error messages only render when errors exist

### Browser Compatibility
- Uses standard DOM APIs (works in all modern browsers)
- Fallback regex for HTML stripping (if DOM not available)
- No browser-specific code

### Mobile Considerations
- Error alert scrolls with content
- Touch-friendly button sizes
- Responsive text sizing

---

## Future Enhancements

### Potential Improvements:
1. **Inline Field Errors**: Show errors directly below each field instead of just at top
2. **Error Tooltips**: Hover over disabled submit button to see errors
3. **Error Focus**: Auto-focus first field with error
4. **Progressive Validation**: Only show errors after user interacts with field
5. **Success Feedback**: Green checkmarks for valid fields

---

## Related Files

- **Component**: `frontend/src/components/SurveyBuilder/QuestionEditor.tsx`
- **Schema**: `frontend/src/schemas/questionSchemas.ts`
- **Types**: `frontend/src/types/index.ts`
- **Documentation**: `FRONTEND_ADD_QUESTION_FIX_IMPLEMENTATION_REPORT.md`

---

**Last Updated**: 2025-11-22
**Component Version**: 1.3.0
**Status**: Production
