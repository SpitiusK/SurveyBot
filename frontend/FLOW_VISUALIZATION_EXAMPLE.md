# Flow Visualization Component - Visual Examples

**Component**: FlowVisualization
**Location**: `frontend/src/components/SurveyBuilder/FlowVisualization.tsx`
**Purpose**: Show survey question flow in Review Step

---

## Example 1: Survey with Branching (SingleChoice)

### Survey Structure
- **Q1**: "What is your experience level?" (SingleChoice)
  - "Beginner" → Q2
  - "Intermediate" → Q3
  - "Expert" → End Survey
- **Q2**: "Do you need basic training?" (Text)
  - All answers → End Survey
- **Q3**: "What topics interest you?" (MultipleChoice)
  - All answers → End Survey

### Visual Output

```
┌─────────────────────────────────────────────────────────────┐
│ Survey Flow Diagram                                         │
│ This diagram shows how respondents will navigate through    │
│ your survey based on their answers.                         │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ [Q1] What is your experience level?   [SingleChoice]   │ │
│ │   ├─ "Beginner" → [Q2]                                 │ │
│ │   ├─ "Intermediate" → [Q3]                             │ │
│ │   └─ "Expert" → End Survey ✓                           │ │
│ └─────────────────────────────────────────────────────────┘ │
│                                                             │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ [Q2] Do you need basic training?   [Text]              │ │
│ │   └─ All answers → End Survey ✓                        │ │
│ └─────────────────────────────────────────────────────────┘ │
│                                                             │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ [Q3] What topics interest you?   [MultipleChoice]      │ │
│ │   └─ All answers → End Survey ✓                        │ │
│ └─────────────────────────────────────────────────────────┘ │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│ ✓ Flow configuration complete. Survey will follow these    │
│   paths based on respondent answers.                        │
└─────────────────────────────────────────────────────────────┘
```

---

## Example 2: Survey with Rating Question Branching

### Survey Structure
- **Q1**: "How satisfied are you with our service?" (Rating 1-5)
  - Rating 1-2 → Q2 (Complaint form)
  - Rating 3 → Q3 (Improvement suggestions)
  - Rating 4-5 → End Survey (Happy customers exit)
- **Q2**: "What went wrong?" (Text)
  - All answers → End Survey
- **Q3**: "How can we improve?" (Text)
  - All answers → End Survey

### Visual Output

```
┌─────────────────────────────────────────────────────────────┐
│ Survey Flow Diagram                                         │
│ This diagram shows how respondents will navigate through    │
│ your survey based on their answers.                         │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ [Q1] How satisfied are you with our service?  [Rating] │ │
│ │   ├─ "1" → [Q2]                                        │ │
│ │   ├─ "2" → [Q2]                                        │ │
│ │   ├─ "3" → [Q3]                                        │ │
│ │   ├─ "4" → End Survey ✓                                │ │
│ │   └─ "5" → End Survey ✓                                │ │
│ └─────────────────────────────────────────────────────────┘ │
│                                                             │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ [Q2] What went wrong?   [Text]                         │ │
│ │   └─ All answers → End Survey ✓                        │ │
│ └─────────────────────────────────────────────────────────┘ │
│                                                             │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ [Q3] How can we improve?   [Text]                      │ │
│ │   └─ All answers → End Survey ✓                        │ │
│ └─────────────────────────────────────────────────────────┘ │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│ ✓ Flow configuration complete. Survey will follow these    │
│   paths based on respondent answers.                        │
└─────────────────────────────────────────────────────────────┘
```

---

## Example 3: Sequential Survey (No Branching)

### Survey Structure
- **Q1**: "What is your name?" (Text)
- **Q2**: "What is your email?" (Text)
- **Q3**: "What topics are you interested in?" (MultipleChoice)

### Visual Output

```
┌─────────────────────────────────────────────────────────────┐
│ ℹ Sequential Flow (No Branching)                           │
│                                                             │
│ Questions will appear in order: Q1 → Q2 → Q3 → ... → End   │
└─────────────────────────────────────────────────────────────┘
```

**Note**: When no flow configuration exists, the component displays a simple informational message rather than the full diagram.

---

## Example 4: Mixed Configuration

### Survey Structure
- **Q1**: "Are you a new customer?" (SingleChoice)
  - "Yes" → Q2
  - "No" → Q3
- **Q2**: "How did you hear about us?" (Text)
  - *No flow configured* → Sequential (next is Q3)
- **Q3**: "What products interest you?" (MultipleChoice)
  - All answers → End Survey

### Visual Output

```
┌─────────────────────────────────────────────────────────────┐
│ Survey Flow Diagram                                         │
│ This diagram shows how respondents will navigate through    │
│ your survey based on their answers.                         │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ [Q1] Are you a new customer?   [SingleChoice]          │ │
│ │   ├─ "Yes" → [Q2]                                      │ │
│ │   └─ "No" → [Q3]                                       │ │
│ └─────────────────────────────────────────────────────────┘ │
│                                                             │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ [Q2] How did you hear about us?   [Text]               │ │
│ │   └─ Sequential (next question in order)               │ │
│ └─────────────────────────────────────────────────────────┘ │
│                                                             │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ [Q3] What products interest you?   [MultipleChoice]    │ │
│ │   └─ All answers → End Survey ✓                        │ │
│ └─────────────────────────────────────────────────────────┘ │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│ ✓ Flow configuration complete. Survey will follow these    │
│   paths based on respondent answers.                        │
└─────────────────────────────────────────────────────────────┘
```

---

## Color Legend

### Question Cards
- **Border**: Light gray (#divider)
- **Background**: White (#background.paper)
- **Question Badge (Q1, Q2...)**: Blue (#primary)
- **Type Badge**: Outlined blue

### Flow Indicators
- **Branching Flow Border**: Primary blue (#primary.light)
- **Default Flow Border**: Gray (#grey.400)
- **Arrow Icons**: Gray action color
- **Next Question Badge**: Outlined blue
- **End Survey Badge**: Success green with checkmark icon

### Alerts
- **Sequential Message**: Info blue
- **Success Summary**: Success green

---

## Responsive Behavior

### Desktop (≥960px)
- Full width flow diagram
- All content visible without scrolling
- Adequate spacing between elements

### Tablet (600-960px)
- Slightly reduced padding
- Flow indentation maintained
- Option text may wrap if very long

### Mobile (<600px)
- Compact padding
- Flow indentation reduced
- Question numbers stack with text
- Option text wraps

---

## Accessibility Features

### Semantic HTML
- Proper heading hierarchy (h6 for title)
- Descriptive text for all elements
- ARIA-compliant Material-UI components

### Visual Clarity
- High contrast text (primary vs secondary)
- Clear visual hierarchy
- Icon + text for end survey markers
- Color is not the only differentiator (icons used)

### Screen Reader Support
- Meaningful labels for all chips
- Descriptive alert messages
- Proper reading order (top to bottom)

---

## User Interaction

### What Users Can Do
- **Read**: Review complete flow logic
- **Verify**: Check all paths are correct
- **Understand**: See where survey ends for each path

### What Users Cannot Do
- **Edit**: Flow editing is in Questions Step only
- **Click**: Component is read-only visualization
- **Export**: No diagram export (future enhancement)

---

## Integration Context

### Where It Appears
**Page**: SurveyBuilder
**Step**: Review & Publish (Step 3 of 3)
**Position**: After SurveyPreview, before action buttons

### User Journey
1. User completes Basic Info step
2. User adds questions and configures flow in Questions step
3. User reaches Review step
4. **FlowVisualization displays** → User verifies flow logic
5. User clicks "Publish Survey"

---

## Technical Details

### Component Props
```typescript
interface FlowVisualizationProps {
  questions: QuestionDraft[];
}
```

### QuestionDraft Structure (Relevant Fields)
```typescript
interface QuestionDraft {
  id: string; // UUID
  questionText: string; // HTML string
  questionType: QuestionType; // 0=Text, 1=SingleChoice, 2=MultipleChoice, 3=Rating
  options: string[]; // For choice questions
  orderIndex: number; // 0-based position
  defaultNextQuestionId?: string | null; // For Text/MultipleChoice/Rating
  optionNextQuestions?: Record<number, string | null>; // For SingleChoice
}
```

### Flow Resolution Logic
1. **SingleChoice/Rating with optionNextQuestions**: Show each option's path
2. **Text/MultipleChoice/Rating with defaultNextQuestionId**: Show "All answers" path
3. **No configuration**: Show "Sequential" message
4. **Null nextQuestionId**: Show "End Survey"

---

## Edge Cases Handled

### Empty States
- ✅ No questions: Component not rendered (ReviewStep handles)
- ✅ No flow configuration: Shows sequential message
- ✅ Empty optionNextQuestions: Shows sequential for that question

### Data Issues
- ✅ Invalid question ID in flow: Gracefully shows "End Survey"
- ✅ Missing options array: Uses optional chaining (`?.`)
- ✅ Very long question text: Truncates at 60 characters
- ✅ HTML in question text: Strips tags for preview

### Flow Scenarios
- ✅ All questions end survey: Valid, shows all as "End Survey"
- ✅ Orphaned questions: Visible in diagram (user can spot issue)
- ✅ Circular references: Not validated here (backend responsibility)

---

## Performance Considerations

### Efficient Rendering
- Pure component (no side effects)
- No state management
- Helper functions are inline (re-created on render, but simple)
- No expensive computations

### Scalability
- Works well for surveys up to 50 questions (schema limit)
- Linear complexity O(n) for rendering
- No nested iterations (except option mapping)

### Memory
- No data stored
- No event listeners
- No timers or intervals
- Lightweight component

---

## Testing Scenarios

### Manual Testing Checklist
- [ ] Sequential survey displays info message
- [ ] SingleChoice branching shows all option paths
- [ ] Rating question shows all rating paths
- [ ] Text question shows "All answers" path
- [ ] MultipleChoice shows "All answers" path
- [ ] "End Survey" markers appear correctly
- [ ] Question numbering is correct
- [ ] Question text truncates properly
- [ ] Question types display correctly
- [ ] Visual hierarchy is clear
- [ ] Colors are consistent with theme
- [ ] Responsive on mobile/tablet/desktop

### Automated Testing (Future)
```typescript
describe('FlowVisualization', () => {
  it('shows sequential message when no flow configured', () => {});
  it('displays SingleChoice branching correctly', () => {});
  it('displays Rating branching correctly', () => {});
  it('displays default flow for Text questions', () => {});
  it('shows End Survey markers correctly', () => {});
  it('handles invalid question IDs gracefully', () => {});
  it('truncates long question text', () => {});
  it('strips HTML from question text', () => {});
});
```

---

## Related Components

### Same Feature
- **SurveyPreview**: Shows survey content (questions, options)
- **FlowConfiguration**: Configures flow in Questions step
- **ReviewStep**: Parent component containing FlowVisualization

### Similar Patterns
- **QuestionList**: Displays questions with drag-and-drop
- **QuestionCard**: Individual question preview card
- **SurveyFlowConfiguration**: Full flow editor page

---

**Implementation**: Complete ✅
**Documentation**: Complete ✅
**Status**: Ready for user testing
