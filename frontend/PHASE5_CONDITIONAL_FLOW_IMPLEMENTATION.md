# Phase 5: Conditional Question Flow - Frontend Implementation

**Version**: 1.0.0
**Date**: 2025-11-21
**Status**: ✅ Complete

---

## Overview

This document details the frontend implementation for Phase 5 of the Conditional Question Flow feature. The frontend provides a user-friendly interface for configuring, visualizing, and validating conditional question flows in surveys.

---

## Features Implemented

### 1. Question Flow Service (`questionFlowService.ts`)

**Location**: `frontend/src/services/questionFlowService.ts`

A TypeScript service providing type-safe API communication with the backend flow endpoints.

**Methods**:
- `getQuestionFlow(surveyId, questionId)` - Retrieve flow configuration for a question
- `updateQuestionFlow(surveyId, questionId, dto)` - Update flow configuration
- `validateSurveyFlow(surveyId)` - Validate entire survey flow for cycles and errors
- `deleteQuestionFlow(surveyId, questionId)` - Remove flow configuration

**Features**:
- Full TypeScript typing
- Error handling
- Integration with centralized axios client (automatic JWT token attachment)
- JSDoc documentation

---

### 2. TypeScript Types & DTOs (`types/index.ts`)

**Added Types**:

```typescript
// Question option with flow configuration
interface QuestionOption {
  id: number;
  text: string;
  orderIndex: number;
  nextQuestionId?: number | null;
}

// Option flow mapping (for API responses)
interface OptionFlowDto {
  optionId: number;
  optionText: string;
  nextQuestionId: number | null;
  isEndOfSurvey: boolean;
}

// Complete flow configuration for a question
interface ConditionalFlowDto {
  questionId: number;
  supportsBranching: boolean;
  defaultNextQuestionId?: number | null;
  optionFlows: OptionFlowDto[];
}

// Flow update payload
interface UpdateQuestionFlowDto {
  defaultNextQuestionId?: number | null;
  optionNextQuestions?: Record<number, number>; // optionId -> nextQuestionId
}

// Validation result
interface SurveyValidationResult {
  valid: boolean;
  errors?: string[];
  cyclePath?: number[]; // Question IDs forming the cycle
}
```

These types ensure type safety across all flow-related operations.

---

### 3. FlowConfigurationPanel Component

**Location**: `frontend/src/components/Surveys/FlowConfigurationPanel.tsx`

**Purpose**: UI for configuring conditional question flow (branching options and next questions)

**Props**:
```typescript
interface FlowConfigurationPanelProps {
  surveyId: number;
  question: Question;
  allQuestions: Question[];
  onFlowUpdated: () => void;
}
```

**Features**:
- **Adaptive UI**: Different interface for branching vs. non-branching questions
- **Branching Questions** (SingleChoice, Rating):
  - Individual next question selector for each option
  - Support for "End Survey" option
- **Non-Branching Questions** (Text, MultipleChoice):
  - Single default next question selector
  - Support for "End Survey" option
- **Validation**: Prevents self-reference (question can't point to itself)
- **State Management**: Loading, error, and success states
- **Actions**: Save, Reset, Remove Flow
- **Real-time Feedback**: Shows success/error messages

**UI Components Used**:
- Material-UI Card, Select, Button, Alert, Chip
- Loading spinners during save operations
- Clear error messaging

**Example Usage**:
```typescript
<FlowConfigurationPanel
  surveyId={surveyId}
  question={selectedQuestion}
  allQuestions={allQuestions}
  onFlowUpdated={() => validateFlow()}
/>
```

---

### 4. FlowVisualization Component

**Location**: `frontend/src/components/Surveys/FlowVisualization.tsx`

**Purpose**: Tree view showing survey question flow structure

**Props**:
```typescript
interface FlowVisualizationProps {
  surveyId: number;
  questions: Question[];
}
```

**Features**:
- **Tree Visualization**: Hierarchical display of question flow
- **Expandable/Collapsible Nodes**: Click to expand/collapse question branches
- **Color Coding**:
  - 🟢 **Green**: Endpoints (questions leading to survey end)
  - 🔵 **Blue**: Questions with flow configured
  - ⚪ **Gray**: Questions with no flow (default order)
  - 🔴 **Red**: Questions with validation errors (in cycle)
- **Branching Display**: Shows which options lead to which questions
- **Cycle Detection**: Visual indicator when cycles are detected
- **Validation Integration**: Displays validation status from API
- **Recursive Rendering**: Handles deeply nested flow structures

**Visual Example**:
```
Q1: Are you 18? (SingleChoice)
  ├─ Option "Yes" → Q2
  └─ Option "No" → Q3

Q2: What age group? (Text)
  → Q4

Q3: Parent consent? (SingleChoice)
  ├─ Option "Yes" → Q4
  └─ Option "No" → END ✓

Q4: Feedback? (Rating)
  → END ✓
```

**Example Usage**:
```typescript
<FlowVisualization
  surveyId={surveyId}
  questions={questions}
/>
```

---

### 5. FlowValidationWarning Component

**Location**: `frontend/src/components/Surveys/FlowValidationWarning.tsx`

**Purpose**: Displays validation status and warnings before survey activation

**Props**:
```typescript
interface FlowValidationWarningProps {
  surveyId: number;
  onFixClick?: () => void;
  autoValidate?: boolean; // Default: true
}
```

**Features**:
- **Auto-validation**: Automatically validates on mount
- **Collapsible Details**: Expandable error details
- **Fix Button**: Optional callback to navigate to flow configuration
- **Cycle Visualization**: Shows cycle path if detected
- **Color-coded Alerts**:
  - Green (Success): Flow is valid
  - Yellow (Warning): Flow has issues
- **User Guidance**: Provides instructions on how to fix issues

**Usage Scenarios**:
- Survey activation dialog
- Survey edit page
- Before publishing survey

**Example Usage**:
```typescript
<FlowValidationWarning
  surveyId={surveyId}
  onFixClick={() => navigate(`/dashboard/surveys/${surveyId}/flow`)}
  autoValidate={true}
/>
```

---

### 6. SurveyFlowConfiguration Page

**Location**: `frontend/src/pages/SurveyFlowConfiguration.tsx`

**Purpose**: Dedicated page for configuring conditional question flow

**Route**: `/dashboard/surveys/:id/flow`

**Layout**:
```
┌─────────────────────────────────────────────────────────────┐
│ Header: Survey Title + Breadcrumbs                          │
├─────────────────────────────────────────────────────────────┤
│ Validation Status Banner (if errors)                        │
├─────────────────────────────────────────────────────────────┤
│ Question Selector (Chip buttons for each question)          │
├──────────────────────────┬──────────────────────────────────┤
│ LEFT: FlowConfigPanel    │ RIGHT: FlowVisualization         │
│ - Configure selected Q   │ - Tree view of entire flow       │
│ - Set next questions     │ - Visual validation feedback     │
│ - Save/Reset/Delete      │ - Expandable nodes               │
└──────────────────────────┴──────────────────────────────────┘
```

**Features**:
- **Side-by-side Layout**: Configuration panel + visualization
- **Question Selection**: Click chips to select question to configure
- **Real-time Validation**: Updates validation after each save
- **Responsive Design**: Adapts to mobile/tablet/desktop
- **Navigation**: Back button, breadcrumbs

**User Flow**:
1. Navigate to flow configuration page
2. Select a question from the question selector
3. Configure flow in left panel
4. See real-time visualization in right panel
5. Validate flow before finishing
6. Navigate back to survey

---

## Routing

**New Route Added**:

```typescript
{
  path: ':id/flow',
  element: <SurveyFlowConfiguration />,
}
```

**Full Path**: `/dashboard/surveys/:id/flow`

**Access**: Protected route (requires authentication)

---

## Integration Points

### How to Access Flow Configuration

1. **From Survey List**:
   - Add a "Configure Flow" button to survey action menu
   - Navigate to `/dashboard/surveys/${surveyId}/flow`

2. **From Survey Edit**:
   - Add a "Flow Configuration" tab or button
   - Navigate to flow page after survey is created

3. **Before Activation**:
   - Show `FlowValidationWarning` component
   - Prevent activation if validation fails
   - Provide "Fix" button that navigates to flow page

### Example: Adding Flow Button to Survey List

```typescript
// In SurveyList.tsx or SurveyCard.tsx
import { useNavigate } from 'react-router-dom';
import { AccountTree as FlowIcon } from '@mui/icons-material';

const navigate = useNavigate();

<Button
  startIcon={<FlowIcon />}
  onClick={() => navigate(`/dashboard/surveys/${survey.id}/flow`)}
>
  Configure Flow
</Button>
```

### Example: Validation Before Activation

```typescript
// In survey activation handler
import { FlowValidationWarning } from '@/components/Surveys';

const [showValidation, setShowValidation] = useState(false);

const handleActivate = async () => {
  // First, validate flow
  const validation = await questionFlowService.validateSurveyFlow(surveyId);

  if (!validation.valid) {
    setShowValidation(true);
    return; // Block activation
  }

  // Proceed with activation
  await surveyService.activateSurvey(surveyId);
};

// In JSX
{showValidation && (
  <FlowValidationWarning
    surveyId={surveyId}
    onFixClick={() => navigate(`/dashboard/surveys/${surveyId}/flow`)}
  />
)}
```

---

## API Endpoints Used

The frontend communicates with these backend endpoints:

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/api/surveys/{surveyId}/questions/{questionId}/flow` | Get flow config |
| PUT | `/api/surveys/{surveyId}/questions/{questionId}/flow` | Update flow config |
| DELETE | `/api/surveys/{surveyId}/questions/{questionId}/flow` | Remove flow config |
| POST | `/api/surveys/{surveyId}/questions/validate` | Validate survey flow |

All endpoints require JWT authentication (automatically handled by axios interceptor).

---

## User Experience Flow

### Configuring Flow

1. **Navigate to Flow Page**: Click "Configure Flow" button on survey
2. **See Validation Status**: Automatic validation on page load
3. **Select Question**: Click question chip from selector
4. **Configure Flow**:
   - For branching questions: Set next question for each option
   - For non-branching: Set default next question
   - Use "End Survey" option to terminate flow
5. **Save Configuration**: Click "Save Flow" button
6. **See Updated Visualization**: Right panel updates in real-time
7. **Validate**: Check validation banner for errors
8. **Fix Errors** (if any): Select problematic questions and fix flow
9. **Complete**: Navigate back when satisfied

### Validation Warnings

- **Green Alert**: Flow is valid, ready to activate
- **Yellow Alert**: Flow has issues, requires fixes
  - Click expand icon to see error details
  - Click "Fix" button to go to flow configuration
  - See cycle path visualization
  - Follow fix instructions

---

## UI/UX Principles

### Design Decisions

1. **Side-by-side Layout**: Configuration + visualization in same view for immediate feedback
2. **Color Coding**: Consistent color scheme across components (green = endpoint, blue = configured, red = error)
3. **Progressive Disclosure**: Expandable/collapsible sections to reduce cognitive load
4. **Real-time Feedback**: Validation and visualization update immediately after changes
5. **Defensive Design**: Prevent invalid actions (e.g., self-reference, missing options)
6. **Clear Messaging**: User-friendly error messages with actionable guidance

### Accessibility

- **Keyboard Navigation**: All interactive elements accessible via keyboard
- **ARIA Labels**: Proper labeling for screen readers
- **Color + Text**: Don't rely on color alone (icons + text for status)
- **Focus Management**: Clear focus indicators

### Responsive Design

- **Mobile**: Stacked layout (config panel above visualization)
- **Tablet**: Side-by-side with adjusted spacing
- **Desktop**: Full side-by-side layout with optimal spacing

---

## Testing Recommendations

### Manual Testing Checklist

- [ ] **Flow Configuration Panel**
  - [ ] Branching question shows option-specific selectors
  - [ ] Non-branching question shows single selector
  - [ ] "End Survey" option works correctly
  - [ ] Self-reference is prevented
  - [ ] Save button saves configuration
  - [ ] Reset button restores original values
  - [ ] Remove flow button deletes configuration
  - [ ] Success/error messages display correctly

- [ ] **Flow Visualization**
  - [ ] Tree view renders correctly
  - [ ] Expand/collapse nodes work
  - [ ] Color coding is correct
  - [ ] Cycle detection highlights errors
  - [ ] Branching paths display correctly
  - [ ] Endpoint indicators are accurate

- [ ] **Validation Warning**
  - [ ] Auto-validates on mount
  - [ ] Shows success for valid flows
  - [ ] Shows warnings for invalid flows
  - [ ] Expand/collapse details work
  - [ ] Fix button navigates correctly
  - [ ] Cycle path is displayed

- [ ] **Flow Configuration Page**
  - [ ] Page loads without errors
  - [ ] Question selector works
  - [ ] Selected question highlights
  - [ ] Both panels update correctly
  - [ ] Validation banner updates after save
  - [ ] Back button navigates correctly
  - [ ] Responsive on all screen sizes

### Integration Testing

- [ ] Service methods call correct API endpoints
- [ ] Type safety is maintained throughout
- [ ] Authentication works (JWT token attached)
- [ ] Error handling works for API failures
- [ ] Loading states display during API calls

---

## Known Limitations

1. **Draft Question Flow**: Flow configuration only works on published questions (not draft questions in builder)
2. **Large Surveys**: Visualization may become complex for surveys with many questions (consider pagination/filtering)
3. **Mobile UX**: Tree visualization may be challenging on small screens (consider alternative mobile view)

---

## Future Enhancements

### Potential Improvements

1. **Visual Graph Editor**: Drag-and-drop node-based flow editor (like flowchart)
2. **Flow Templates**: Pre-built flow patterns (e.g., age-gated surveys, conditional paths)
3. **Flow Analytics**: Show which paths users take most often
4. **Bulk Edit**: Configure multiple questions at once
5. **Flow Preview**: Test survey flow without activating
6. **Export/Import**: Export flow configuration as JSON, import to other surveys
7. **Undo/Redo**: History of flow changes
8. **Flow Comments**: Add notes/comments to explain complex flow logic

---

## Developer Notes

### Adding Flow Configuration to Existing Pages

**Option 1: Add Button to Survey List**

```typescript
// In SurveyTable.tsx or SurveyCard.tsx
import { AccountTree } from '@mui/icons-material';

<IconButton onClick={() => navigate(`/dashboard/surveys/${survey.id}/flow`)}>
  <AccountTree />
</IconButton>
```

**Option 2: Add to Survey Detail Page**

```typescript
// Create a tabs layout in survey detail page
<Tabs>
  <Tab label="Overview" />
  <Tab label="Questions" />
  <Tab label="Flow Configuration" />
  <Tab label="Statistics" />
</Tabs>
```

**Option 3: Integrate into Survey Builder**

Add a 4th step to the wizard:
```typescript
STEPS = [
  'basic-info',
  'questions',
  'flow-configuration', // New step
  'review'
]
```

### Using Flow Service in Other Components

```typescript
import questionFlowService from '@/services/questionFlowService';

// Get flow config
const flow = await questionFlowService.getQuestionFlow(surveyId, questionId);

// Update flow
await questionFlowService.updateQuestionFlow(surveyId, questionId, {
  defaultNextQuestionId: 5,
});

// Validate
const validation = await questionFlowService.validateSurveyFlow(surveyId);
if (!validation.valid) {
  console.error('Validation errors:', validation.errors);
}
```

---

## Files Created/Modified

### New Files Created

1. `frontend/src/services/questionFlowService.ts` - Flow API service
2. `frontend/src/components/Surveys/FlowConfigurationPanel.tsx` - Configuration UI
3. `frontend/src/components/Surveys/FlowVisualization.tsx` - Tree visualization
4. `frontend/src/components/Surveys/FlowValidationWarning.tsx` - Validation alerts
5. `frontend/src/components/Surveys/index.ts` - Component exports
6. `frontend/src/pages/SurveyFlowConfiguration.tsx` - Flow configuration page
7. `frontend/PHASE5_CONDITIONAL_FLOW_IMPLEMENTATION.md` - This documentation

### Modified Files

1. `frontend/src/types/index.ts` - Added flow-related TypeScript types
2. `frontend/src/routes/index.tsx` - Added `/dashboard/surveys/:id/flow` route

---

## Summary

Phase 5 Frontend implementation provides a complete, user-friendly interface for:
- ✅ Configuring conditional question flow (branching and default paths)
- ✅ Visualizing survey flow structure (tree view with color coding)
- ✅ Validating flow for cycles and logical errors
- ✅ Warning users before survey activation
- ✅ Type-safe API integration with backend endpoints

The implementation follows React best practices, Material-UI design patterns, and provides excellent UX with real-time feedback, clear error messaging, and intuitive controls.

**Next Steps**: Integrate flow configuration buttons into existing survey management pages (SurveyList, SurveyEdit) and add validation checks before survey activation.

---

**End of Phase 5 Frontend Implementation Documentation**

**Last Updated**: 2025-11-21
**Version**: 1.0.0
